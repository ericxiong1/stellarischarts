using StellarisCharts.Api.Data;
using StellarisCharts.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using DotNetEnv;

string? envLoadedFrom = null;
string? FindEnvUpwards(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (File.Exists(candidate))
            return candidate;
        dir = dir.Parent;
    }
    return null;
}

var envPath = FindEnvUpwards(Directory.GetCurrentDirectory())
    ?? FindEnvUpwards(AppContext.BaseDirectory);

if (envPath != null)
{
    Env.Load(envPath);
    envLoadedFrom = envPath;
}
var builder = WebApplication.CreateBuilder(args);

var envConnection =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
    Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
if (!string.IsNullOrWhiteSpace(envConnection))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = envConnection;
}

// Configure Kestrel to accept large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddScoped<SaveFileParserService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
});

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Missing DefaultConnection. envLoadedFrom={envLoadedFrom ?? "none"}, " +
        $"envVar={(string.IsNullOrWhiteSpace(envConnection) ? "missing" : "present")}");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowReactApp");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Skip HTTPS redirection in development
}
else
{
    app.UseHttpsRedirection();
}

// Upload and parse Stellaris save file
app.MapPost("/api/saves/upload", async (HttpRequest request, IFormFile file, SaveFileParserService parser, AppDbContext db) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file provided");

    try
    {
        var snapshotTime = DateTime.UtcNow;
        if (request.Headers.TryGetValue("X-File-Timestamp", out var timestampHeader))
        {
            if (DateTimeOffset.TryParse(timestampHeader.ToString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            {
                snapshotTime = timestamp.UtcDateTime;
            }
        }

        var content = await ReadGamestateContentAsync(file);
        
        var result = parser.ParseGamestate(content);
        
        // Save countries (dedupe in-memory and avoid N+1 queries)
        var distinctCountries = result.Countries
            .GroupBy(c => c.CountryId)
            .Select(g => g.First())
            .ToList();

        var incomingIds = distinctCountries.Select(c => c.CountryId).ToList();
        var existingIds = await db.Countries
            .Where(c => incomingIds.Contains(c.CountryId))
            .Select(c => c.CountryId)
            .ToListAsync();

        var existingCountries = await db.Countries
            .Where(c => incomingIds.Contains(c.CountryId))
            .ToListAsync();

        var existingById = existingCountries.ToDictionary(c => c.CountryId);

        foreach (var country in distinctCountries)
        {
            if (!existingIds.Contains(country.CountryId))
            {
                db.Countries.Add(country);
                continue;
            }

            if (existingById.TryGetValue(country.CountryId, out var existing))
            {
                // Keep names in sync with latest save data
                existing.Name = country.Name;
                existing.Adjective = country.Adjective;
                existing.GovernmentType = country.GovernmentType;
                existing.Authority = country.Authority;
                existing.Ethos = country.Ethos;
                existing.Civics = country.Civics;
                existing.TraditionTrees = country.TraditionTrees;
                existing.AscensionPerks = country.AscensionPerks;
                existing.FederationType = country.FederationType;
                existing.SubjectStatus = country.SubjectStatus;
                existing.DiplomaticStance = country.DiplomaticStance;
                existing.DiplomaticWeight = country.DiplomaticWeight;
                existing.Personality = country.Personality;
                existing.GraphicalCulture = country.GraphicalCulture;
                existing.Capital = country.Capital;
                existing.MilitaryPower = country.MilitaryPower;
                existing.EconomyPower = country.EconomyPower;
                existing.TechPower = country.TechPower;
                existing.FleetSize = country.FleetSize;
                existing.EmpireSize = country.EmpireSize;
                existing.NumSapientPops = country.NumSapientPops;
                existing.VictoryRank = country.VictoryRank;
                existing.VictoryScore = country.VictoryScore;
            }
        }
        
        await db.SaveChangesAsync();

        // Replace current war status for incoming countries
        var existingWars = db.WarStatuses.Where(w => incomingIds.Contains(w.CountryId));
        db.WarStatuses.RemoveRange(existingWars);

        if (result.Wars.Count > 0)
        {
            foreach (var war in result.Wars)
            {
                foreach (var attackerId in war.AttackerIds)
                {
                    if (!incomingIds.Contains(attackerId))
                        continue;

                    db.WarStatuses.Add(new StellarisCharts.Api.Models.WarStatus
                    {
                        CountryId = attackerId,
                        WarId = war.WarId,
                        WarName = war.WarName,
                        WarStartDate = war.WarStartDate,
                        WarLength = war.WarLength,
                        AttackerWarExhaustion = war.AttackerWarExhaustion,
                        DefenderWarExhaustion = war.DefenderWarExhaustion,
                        Attackers = war.Attackers,
                        Defenders = war.Defenders
                    });
                }

                foreach (var defenderId in war.DefenderIds)
                {
                    if (!incomingIds.Contains(defenderId))
                        continue;

                    db.WarStatuses.Add(new StellarisCharts.Api.Models.WarStatus
                    {
                        CountryId = defenderId,
                        WarId = war.WarId,
                        WarName = war.WarName,
                        WarStartDate = war.WarStartDate,
                        WarLength = war.WarLength,
                        AttackerWarExhaustion = war.AttackerWarExhaustion,
                        DefenderWarExhaustion = war.DefenderWarExhaustion,
                        Attackers = war.Attackers,
                        Defenders = war.Defenders
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        var snapshots = distinctCountries.Select(c => new StellarisCharts.Api.Models.Snapshot
        {
            CountryId = c.CountryId,
            GameDate = result.GameDate,
            Tick = result.Tick,
            MilitaryPower = c.MilitaryPower,
            EconomyPower = c.EconomyPower,
            TechPower = c.TechPower,
            FleetSize = c.FleetSize,
            EmpireSize = c.EmpireSize,
            NumSapientPops = c.NumSapientPops,
            VictoryRank = c.VictoryRank,
            VictoryScore = c.VictoryScore,
            SnapshotTime = snapshotTime
        }).ToList();

        db.Snapshots.AddRange(snapshots);
        await db.SaveChangesAsync();

        var snapshotIdByCountryId = snapshots.ToDictionary(s => s.CountryId, s => s.Id);
        foreach (var item in result.BudgetLineItems)
        {
            if (snapshotIdByCountryId.TryGetValue(item.CountryId, out var snapshotId))
            {
                item.SnapshotId = snapshotId;
                db.BudgetLineItems.Add(item);
            }
        }

        await db.SaveChangesAsync();

        foreach (var species in result.SpeciesPopulations)
        {
            if (snapshotIdByCountryId.TryGetValue(species.CountryId, out var snapshotId))
            {
                species.SnapshotId = snapshotId;
                db.SpeciesPopulations.Add(species);
            }
        }

        await db.SaveChangesAsync();

        foreach (var species in result.GlobalSpeciesPopulations)
        {
            species.SnapshotId = snapshots.First().Id;
            db.GlobalSpeciesPopulations.Add(species);
        }

        await db.SaveChangesAsync();
        
        return Results.Ok(new
        {
            message = "Save file parsed successfully",
            countries = distinctCountries.Count,
            snapshots = snapshots.Count,
            budgetItems = result.BudgetLineItems.Count,
            gameDate = result.GameDate,
            tick = result.Tick,
            snapshotTime
        });
    }
    catch (DbUpdateException dbEx)
    {
        var detail = dbEx.InnerException?.Message ?? dbEx.Message;
        return Results.BadRequest($"Error parsing file: {detail}");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error parsing file: {ex.Message}");
    }
})
.WithName("UploadSaveFile")
.WithOpenApi()
.DisableAntiforgery();

static async Task<string> ReadGamestateContentAsync(IFormFile file)
{
    if (!string.IsNullOrWhiteSpace(file.FileName) &&
        file.FileName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
    {
        using var archive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.Entries.FirstOrDefault(e =>
            string.Equals(e.Name, "gamestate", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.EndsWith("/gamestate", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.EndsWith("\\gamestate", StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new InvalidOperationException("gamestate entry not found in save file");
        }

        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return await reader.ReadToEndAsync();
    }

    using var fileReader = new StreamReader(file.OpenReadStream());
    return await fileReader.ReadToEndAsync();
}

// Clear uploaded save data
app.MapDelete("/api/saves/clear", async (AppDbContext db) =>
{
    await db.Database.ExecuteSqlRawAsync(@"
DO $$
BEGIN
    IF to_regclass('public.""SpeciesPopulations""') IS NOT NULL THEN
        EXECUTE 'TRUNCATE TABLE ""SpeciesPopulations"" RESTART IDENTITY CASCADE';
    END IF;
    IF to_regclass('public.""GlobalSpeciesPopulations""') IS NOT NULL THEN
        EXECUTE 'TRUNCATE TABLE ""GlobalSpeciesPopulations"" RESTART IDENTITY CASCADE';
    END IF;
    IF to_regclass('public.""WarStatuses""') IS NOT NULL THEN
        EXECUTE 'TRUNCATE TABLE ""WarStatuses"" RESTART IDENTITY CASCADE';
    END IF;
    EXECUTE 'TRUNCATE TABLE ""BudgetLineItems"", ""Snapshots"", ""Countries"" RESTART IDENTITY CASCADE';
END $$;");
    return Results.Ok(new { message = "Save data cleared" });
})
.WithName("ClearSaveData")
.WithOpenApi();

// Test endpoint to parse gamestate directly
app.MapGet("/api/test/parse-gamestate", (SaveFileParserService parser) =>
{
    try
    {
        var filePath = @"C:\Users\Eric\Desktop\stellaris-charts\gamestate";
        if (!System.IO.File.Exists(filePath))
            return Results.BadRequest($"File not found: {filePath}");
        
        var content = System.IO.File.ReadAllText(filePath);
        var result = parser.ParseGamestate(content);
        
        return Results.Ok(new
        {
            countriesCount = result.Countries.Count,
            budgetItemsCount = result.BudgetLineItems.Count,
            countries = result.Countries.Select(c => new
            {
                c.CountryId,
                c.Name,
                c.GovernmentType,
                c.MilitaryPower,
                c.NumSapientPops
            }).ToList(),
            budgetSample = result.BudgetLineItems.Take(10).Select(b => new
            {
                b.CountryId,
                b.Section,
                b.Category,
                b.ResourceType,
                b.Amount
            }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}\n{ex.StackTrace}");
    }
})
.WithName("TestParseGamestate")
.WithOpenApi();

// Get all countries
app.MapGet("/api/countries", async (AppDbContext db) =>
{
    return await db.Countries.ToListAsync();
})
.WithName("GetCountries")
.WithOpenApi();

// Get countries with latest income totals
app.MapGet("/api/countries/summary", async (AppDbContext db) =>
{
    var countries = await db.Countries.ToListAsync();
    if (countries.Count == 0)
        return Results.Ok(new List<object>());

    var latestSnapshots = await db.Snapshots
        .GroupBy(s => s.CountryId)
        .Select(g => g.OrderByDescending(s => s.SnapshotTime).First())
        .ToListAsync();

    var snapshotIds = latestSnapshots.Select(s => s.Id).ToList();
    var budgetItems = await db.BudgetLineItems
        .Where(b => snapshotIds.Contains(b.SnapshotId) && (b.Section == "income" || b.Section == "expenses"))
        .ToListAsync();

    var netBySnapshot = budgetItems
        .GroupBy(b => b.SnapshotId)
        .ToDictionary(
            g => g.Key,
            g => g.GroupBy(i => i.ResourceType)
                .ToDictionary(
                    gg => gg.Key,
                    gg => gg.Sum(x => x.Section == "income" ? x.Amount : -x.Amount)
                )
        );

    var latestSnapshotByCountry = latestSnapshots.ToDictionary(s => s.CountryId, s => s.Id);

    var result = countries.Select(c =>
    {
        netBySnapshot.TryGetValue(latestSnapshotByCountry.GetValueOrDefault(c.CountryId), out var totals);
        return new
        {
            country = new
            {
                c.Id,
                c.Name,
                c.Adjective,
                c.CountryId,
                c.GovernmentType,
                c.Authority,
                c.Ethos,
                c.Civics,
                c.TraditionTrees,
                c.AscensionPerks,
                c.FederationType,
                c.SubjectStatus,
                c.DiplomaticStance,
                c.DiplomaticWeight,
                c.Personality,
                c.GraphicalCulture,
                c.Capital,
                c.MilitaryPower,
                c.EconomyPower,
                c.TechPower,
                c.FleetSize,
                c.EmpireSize,
                c.NumSapientPops,
                c.VictoryRank,
                c.VictoryScore,
                c.CreatedAt
            },
            incomeTotals = totals ?? new Dictionary<string, decimal>()
        };
    });

    return Results.Ok(result);
})
.WithName("GetCountrySummaries")
.WithOpenApi();

// Galaxywide net resource totals from latest snapshots
app.MapGet("/api/galaxy/summary", async (AppDbContext db) =>
{
    var recentDates = await db.Snapshots
        .Select(s => s.GameDate)
        .Distinct()
        .OrderByDescending(d => d)
        .Take(2)
        .ToListAsync();

    if (recentDates.Count == 0)
        return Results.Ok(new { totals = new Dictionary<string, decimal>(), previousTotals = new Dictionary<string, decimal>() });

    var currentDate = recentDates[0];
    var previousDate = recentDates.Count > 1 ? recentDates[1] : null;

    async Task<(Dictionary<string, decimal> net, Dictionary<string, decimal> income, Dictionary<string, decimal> expense)> TotalsForDate(string date)
    {
        var snapshotsForDate = await db.Snapshots
            .Where(s => s.GameDate == date)
            .GroupBy(s => s.CountryId)
            .Select(g => g.OrderByDescending(s => s.SnapshotTime).First())
            .ToListAsync();

        var snapshotIds = snapshotsForDate.Select(s => s.Id).ToList();
        if (snapshotIds.Count == 0)
            return (new Dictionary<string, decimal>(), new Dictionary<string, decimal>(), new Dictionary<string, decimal>());

        var items = await db.BudgetLineItems
            .Where(b => snapshotIds.Contains(b.SnapshotId) && (b.Section == "income" || b.Section == "expenses"))
            .ToListAsync();

        var income = items
            .Where(i => i.Section == "income")
            .GroupBy(i => i.ResourceType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var expense = items
            .Where(i => i.Section == "expenses")
            .GroupBy(i => i.ResourceType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var net = items
            .GroupBy(i => i.ResourceType)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Section == "income" ? x.Amount : -x.Amount)
            );

        return (net, income, expense);
    }

    var totalsTuple = await TotalsForDate(currentDate);
    var previousTuple = previousDate == null
        ? (new Dictionary<string, decimal>(), new Dictionary<string, decimal>(), new Dictionary<string, decimal>())
        : await TotalsForDate(previousDate);

    return Results.Ok(new
    {
        totals = totalsTuple.net,
        previousTotals = previousTuple.Item1,
        incomeTotals = totalsTuple.income,
        expenseTotals = totalsTuple.expense,
        previousIncomeTotals = previousTuple.Item2,
        previousExpenseTotals = previousTuple.Item3,
        gameDate = currentDate,
        previousGameDate = previousDate
    });
})
.WithName("GetGalaxySummary")
.WithOpenApi();

// Get snapshots for a country
app.MapGet("/api/countries/{id}/snapshots", async (int id, AppDbContext db) =>
{
    return await db.Snapshots
        .Where(s => s.CountryId == id)
        .OrderByDescending(s => s.GameDate)
        .ToListAsync();
})
.WithName("GetCountrySnapshots")
.WithOpenApi();

// Get active wars for a country
app.MapGet("/api/countries/{id}/wars", async (int id, AppDbContext db) =>
{
    return await db.WarStatuses
        .Where(w => w.CountryId == id)
        .ToListAsync();
})
.WithName("GetCountryWars")
.WithOpenApi();

// Get budget data for a snapshot
app.MapGet("/api/snapshots/{id}/budget", async (int id, AppDbContext db) =>
{
    return await db.BudgetLineItems
        .Where(b => b.SnapshotId == id)
        .ToListAsync();
})
.WithName("GetSnapshotBudget")
.WithOpenApi();

// Get species breakdown for a snapshot
app.MapGet("/api/snapshots/{id}/species", async (int id, AppDbContext db) =>
{
    var speciesTable = await db.Database.SqlQueryRaw<string?>(
        "SELECT to_regclass('public.\"SpeciesPopulations\"')::text AS \"Value\"").FirstOrDefaultAsync();
    if (speciesTable == null)
        return Results.Ok(new List<object>());

    var data = await db.SpeciesPopulations
        .Where(sp => sp.SnapshotId == id)
        .GroupBy(sp => sp.SpeciesName)
        .Select(g => new
        {
            speciesName = g.Key,
            amount = g.Sum(x => x.Amount)
        })
        .OrderByDescending(x => x.amount)
        .ToListAsync();

    return Results.Ok(data);
})
.WithName("GetSnapshotSpecies")
.WithOpenApi();

// Galaxywide species breakdown from latest snapshots
app.MapGet("/api/galaxy/species", async (AppDbContext db) =>
{
    var speciesTable = await db.Database.SqlQueryRaw<string?>(
        "SELECT to_regclass('public.\"GlobalSpeciesPopulations\"')::text AS \"Value\"").FirstOrDefaultAsync();
    if (speciesTable == null)
        return Results.Ok(new List<object>());

    var latestDate = await db.Snapshots
        .Select(s => s.GameDate)
        .Distinct()
        .OrderByDescending(d => d)
        .FirstOrDefaultAsync();

    if (latestDate == null)
        return Results.Ok(new List<object>());

    var latestSnapshots = await db.Snapshots
        .Where(s => s.GameDate == latestDate)
        .GroupBy(s => s.CountryId)
        .Select(g => g.OrderByDescending(s => s.SnapshotTime).First())
        .ToListAsync();

    var snapshotIds = latestSnapshots.Select(s => s.Id).ToList();
    if (snapshotIds.Count == 0)
        return Results.Ok(new List<object>());

    var data = await db.GlobalSpeciesPopulations
        .Where(sp => snapshotIds.Contains(sp.SnapshotId))
        .GroupBy(sp => sp.SpeciesName)
        .Select(g => new
        {
            speciesName = g.Key,
            amount = g.Sum(x => x.Amount)
        })
        .OrderByDescending(x => x.amount)
        .ToListAsync();

    return Results.Ok(data);
})
.WithName("GetGalaxySpecies")
.WithOpenApi();

// Galaxywide species breakdown from previous snapshots
app.MapGet("/api/galaxy/species/previous", async (AppDbContext db) =>
{
    var speciesTable = await db.Database.SqlQueryRaw<string?>(
        "SELECT to_regclass('public.\"GlobalSpeciesPopulations\"')::text AS \"Value\"").FirstOrDefaultAsync();
    if (speciesTable == null)
        return Results.Ok(new List<object>());

    var recentDates = await db.Snapshots
        .Select(s => s.GameDate)
        .Distinct()
        .OrderByDescending(d => d)
        .Take(2)
        .ToListAsync();

    if (recentDates.Count < 2)
        return Results.Ok(new List<object>());

    var previousDate = recentDates[1];

    var latestSnapshots = await db.Snapshots
        .Where(s => s.GameDate == previousDate)
        .GroupBy(s => s.CountryId)
        .Select(g => g.OrderByDescending(s => s.SnapshotTime).First())
        .ToListAsync();

    var snapshotIds = latestSnapshots.Select(s => s.Id).ToList();
    if (snapshotIds.Count == 0)
        return Results.Ok(new List<object>());

    var data = await db.GlobalSpeciesPopulations
        .Where(sp => snapshotIds.Contains(sp.SnapshotId))
        .GroupBy(sp => sp.SpeciesName)
        .Select(g => new
        {
            speciesName = g.Key,
            amount = g.Sum(x => x.Amount)
        })
        .OrderByDescending(x => x.amount)
        .ToListAsync();

    return Results.Ok(data);
})
.WithName("GetGalaxySpeciesPrevious")
.WithOpenApi();

// Galaxywide species breakdown history by game date
app.MapGet("/api/galaxy/species/history", async (AppDbContext db) =>
{
    var speciesTable = await db.Database.SqlQueryRaw<string?>(
        "SELECT to_regclass('public.\"GlobalSpeciesPopulations\"')::text AS \"Value\"").FirstOrDefaultAsync();
    if (speciesTable == null)
        return Results.Ok(new List<object>());

    var snapshotIds = await db.GlobalSpeciesPopulations
        .Select(sp => sp.SnapshotId)
        .Distinct()
        .ToListAsync();

    if (snapshotIds.Count == 0)
        return Results.Ok(new List<object>());

    var latestSnapshots = await db.Snapshots
        .Where(s => snapshotIds.Contains(s.Id))
        .GroupBy(s => s.GameDate)
        .Select(g => g.OrderByDescending(s => s.SnapshotTime).First())
        .ToListAsync();

    if (latestSnapshots.Count == 0)
        return Results.Ok(new List<object>());

    var latestIds = latestSnapshots.Select(s => s.Id).ToList();
    var snapshotDateById = latestSnapshots.ToDictionary(s => s.Id, s => s.GameDate);

    var data = await db.GlobalSpeciesPopulations
        .Where(sp => latestIds.Contains(sp.SnapshotId))
        .Select(sp => new
        {
            gameDate = snapshotDateById[sp.SnapshotId],
            speciesName = sp.SpeciesName,
            amount = sp.Amount
        })
        .ToListAsync();

    return Results.Ok(data);
})
.WithName("GetGalaxySpeciesHistory")
.WithOpenApi();

app.Run();
