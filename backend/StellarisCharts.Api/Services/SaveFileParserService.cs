using System.Text.RegularExpressions;
using StellarisCharts.Api.Models;

namespace StellarisCharts.Api.Services;

public class SaveFileParserService
{
    /// <summary>
    /// Parses a Stellaris gamestate file and extracts country and budget data
    /// </summary>
    public ParseResult ParseGamestate(string content)
    {
        var result = new ParseResult();

        result.GameDate = ExtractStringValue(content, @"^date=""([^""]+)""") ?? "Unknown";
        result.Tick = ExtractIntValue(content, @"^tick=(\d+)");
        
        var federationInfo = ParseFederationInfo(content);

        var countryDataById = new Dictionary<int, string>();

        // Extract all countries
        if (TryExtractTopLevelBlock(content, "country", out var countriesContent))
        {
            var countryBlocks = ExtractCountryBlocks(countriesContent);
            countryDataById = countryBlocks.ToDictionary(c => c.id, c => c.data);
            
            foreach (var (countryId, countryData) in countryBlocks)
            {
                var country = ParseCountry(countryId, countryData);
                if (country != null)
                {
                    result.Countries.Add(country);
                    
                    // Extract budget data
                    var budgetItems = ParseBudget(countryId, countryData);
                    result.BudgetLineItems.AddRange(budgetItems);
                }
            }
        }

        if (result.Countries.Count > 0)
        {
            var countryNameById = result.Countries.ToDictionary(c => c.CountryId, c => c.Name);
            var countryAdjectiveById = result.Countries.ToDictionary(c => c.CountryId, c => c.Adjective);

            var federationNames = ResolveFederationNames(federationInfo, countryAdjectiveById);
            foreach (var country in result.Countries)
            {
                if (countryDataById.TryGetValue(country.CountryId, out var data))
                {
                    var federationId = ExtractFederationId(data);
                    if (federationId != 0 && federationNames.TryGetValue(federationId, out var name))
                    {
                        country.FederationType = name;
                    }
                }
            }

            var wars = ParseWars(content, result.GameDate, countryNameById, countryAdjectiveById);
            result.Wars.AddRange(wars);

            foreach (var country in result.Countries)
            {
                if (!string.IsNullOrWhiteSpace(country.SubjectStatus))
                {
                    var subjectMatch = Regex.Match(country.SubjectStatus, @"Subject of:\s*(\d+)");
                    if (subjectMatch.Success && int.TryParse(subjectMatch.Groups[1].Value, out var overlordId))
                    {
                        if (countryNameById.TryGetValue(overlordId, out var name))
                        {
                            country.SubjectStatus = $"Subject of: {name}";
                        }
                    }

                    var overlordMatch = Regex.Match(country.SubjectStatus, @"Overlord of:\s*(.+)");
                    if (overlordMatch.Success)
                    {
                        var ids = Regex.Matches(country.SubjectStatus, @"\b\d+\b")
                            .Select(m => int.TryParse(m.Value, out var id) ? id : 0)
                            .Where(id => id != 0)
                            .ToList();
                        var names = ids
                            .Select(id => countryNameById.TryGetValue(id, out var name) ? name : null)
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        country.SubjectStatus = names.Count == 0
                            ? string.Empty
                            : $"Overlord of: {string.Join(", ", names)}";
                    }
                }
            }

            var allowedCountries = result.Countries.Select(c => c.CountryId).ToHashSet();
            var speciesPopulations = ParseSpeciesDemographics(content, allowedCountries);
            result.SpeciesPopulations.AddRange(speciesPopulations);
        }

        var globalSpecies = ParseGlobalSpeciesDemographics(content);
        result.GlobalSpeciesPopulations.AddRange(globalSpecies);
        
        return result;
    }

    private List<(int id, string data)> ExtractCountryBlocks(string content)
    {
        var blocks = new List<(int, string)>();
        int i = 0;
        int depth = 0;

        while (i < content.Length)
        {
            char ch = content[i];
            if (ch == '{')
            {
                depth++;
                i++;
                continue;
            }
            if (ch == '}')
            {
                depth = Math.Max(0, depth - 1);
                i++;
                continue;
            }

            if (depth == 0 && char.IsDigit(ch))
            {
                int idStart = i;
                while (i < content.Length && char.IsDigit(content[i]))
                    i++;

                if (!int.TryParse(content.AsSpan(idStart, i - idStart), out var countryId))
                    continue;

                while (i < content.Length && char.IsWhiteSpace(content[i]))
                    i++;

                if (i >= content.Length || content[i] != '=')
                    continue;
                i++; // skip '='

                while (i < content.Length && char.IsWhiteSpace(content[i]))
                    i++;

                if (i >= content.Length || content[i] != '{')
                    continue;

                int startBrace = i;
                int braceCount = 1;
                i++; // move past '{'

                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{')
                        braceCount++;
                    else if (content[i] == '}')
                        braceCount--;
                    i++;
                }

                if (braceCount == 0)
                {
                    string countryData = content.Substring(startBrace + 1, i - startBrace - 2);
                    blocks.Add((countryId, countryData));
                }

                continue;
            }

            i++;
        }
        
        return blocks;
    }

    private List<(int id, string data)> ExtractIdBlocks(string content)
    {
        var blocks = new List<(int, string)>();
        int i = 0;
        int depth = 0;

        while (i < content.Length)
        {
            char ch = content[i];
            if (ch == '{')
            {
                depth++;
                i++;
                continue;
            }
            if (ch == '}')
            {
                depth = Math.Max(0, depth - 1);
                i++;
                continue;
            }

            if (depth == 0 && char.IsDigit(ch))
            {
                int idStart = i;
                while (i < content.Length && char.IsDigit(content[i]))
                    i++;

                if (!int.TryParse(content.AsSpan(idStart, i - idStart), out var entityId))
                    continue;

                while (i < content.Length && char.IsWhiteSpace(content[i]))
                    i++;

                if (i >= content.Length || content[i] != '=')
                    continue;
                i++;

                while (i < content.Length && char.IsWhiteSpace(content[i]))
                    i++;

                if (i >= content.Length || content[i] != '{')
                    continue;

                int startBrace = i;
                int braceCount = 1;
                i++;

                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{')
                        braceCount++;
                    else if (content[i] == '}')
                        braceCount--;
                    i++;
                }

                if (braceCount == 0)
                {
                    string entityData = content.Substring(startBrace + 1, i - startBrace - 2);
                    blocks.Add((entityId, entityData));
                }

                continue;
            }

            i++;
        }

        return blocks;
    }

    private Country? ParseCountry(int countryId, string countryData)
    {
        try
        {
            if (IsExcludedCountry(countryData))
            {
                return null;
            }

            var country = new Country
            {
                CountryId = countryId,
                Name = ExtractStringValue(countryData,
                    @"name=\s*\{\s*key=""([^""]+)""",
                    @"name=\s*""([^""]+)""") ?? "Unknown",
                Adjective = ExtractStringValue(countryData,
                    @"adjective=\s*\{\s*key=""([^""]+)""",
                    @"adjective=\s*""([^""]+)""") ?? "Unknown",
                GovernmentType = ExtractStringValue(countryData, @"government=\s*\{\s*key=""([^""]+)""") ?? "Unknown",
                Authority = ExtractStringValue(countryData, @"authority=""([^""]+)""") ?? "Unknown",
                Ethos = ExtractEthos(countryData),
                Civics = ExtractListValue(countryData, "civics", "civic_"),
                TraditionTrees = ExtractListValue(countryData, "tradition_categories", "tradition_"),
                AscensionPerks = ExtractListValue(countryData, "ascension_perks", "ap_"),
                FederationType = string.Empty,
                SubjectStatus = ExtractSubjectStatus(countryData),
                DiplomaticStance = ExtractDiplomaticStance(countryData),
                DiplomaticWeight = ExtractDiplomaticWeight(countryData),
                Personality = ExtractStringValue(countryData, @"personality=""([^""]+)""") ?? "Unknown",
                GraphicalCulture = ExtractStringValue(countryData, @"graphical_culture=""([^""]+)""") ?? "Unknown",
                Capital = ExtractIntValue(countryData, @"capital=(\d+)"),
                MilitaryPower = ExtractDecimalValue(countryData, @"military_power=([0-9.]+)"),
                EconomyPower = ExtractDecimalValue(countryData, @"economy_power=([0-9.]+)"),
                TechPower = ExtractDecimalValue(countryData, @"tech_power=([0-9.]+)"),
                FleetSize = ExtractIntValue(countryData, @"fleet_size=(\d+)"),
                EmpireSize = ExtractIntValue(countryData, @"empire_size=(\d+)"),
                NumSapientPops = ExtractLongValue(countryData, @"num_sapient_pops=(\d+)"),
                VictoryRank = ExtractIntValue(countryData, @"victory_rank=(\d+)"),
                VictoryScore = ExtractDecimalValue(countryData, @"victory_score=([0-9.]+)"),
            };

            if (country.EconomyPower == 1m || country.NumSapientPops == 0)
            {
                return null;
            }

            if (country.Name == "%ADJECTIVE%" || country.Name == "%ADJ%")
            {
                if (TryExtractInlineBlock(countryData, "name", out var nameBlock))
                {
                    var resolved = ExtractNameFromBlock(nameBlock);
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        country.Name = resolved;
                    }
                }
            }

            if (country.Adjective == "%ADJECTIVE%" || country.Adjective == "%ADJ%")
            {
                if (TryExtractInlineBlock(countryData, "adjective", out var adjectiveBlock))
                {
                    var resolved = ExtractAdjectiveFromBlock(adjectiveBlock);
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        country.Adjective = resolved;
                    }
                }
            }

            if (country.GovernmentType == "Unknown")
            {
                if (TryExtractInlineBlock(countryData, "government", out var governmentBlock))
                {
                    var resolved = ExtractStringValue(
                        governmentBlock,
                        @"type=""([^""]+)""",
                        @"key=""([^""]+)""");
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        country.GovernmentType = resolved;
                    }
                }
            }

            if (ShouldExcludeByName(country.Name))
            {
                return null;
            }

            country.Name = NormalizeLocalizationKey(country.Name);
            country.Adjective = NormalizeLocalizationKey(country.Adjective);
            country.Authority = ToHumanLabel(country.Authority, "auth_");
            country.GovernmentType = ToHumanLabel(country.GovernmentType, "gov_");

            if (country.Name.StartsWith("NAME_", StringComparison.Ordinal))
            {
                return null;
            }

            return country;
        }
        catch
        {
            return null;
        }
    }

    private bool TryExtractTopLevelBlock(string content, string key, out string block)
    {
        block = string.Empty;
        var match = Regex.Match(content, @"^" + Regex.Escape(key) + @"\s*=\s*\{", RegexOptions.Multiline);
        if (!match.Success)
            return false;

        int startBrace = match.Index + match.Length - 1;
        int braceCount = 1;
        int pos = startBrace + 1;

        while (pos < content.Length && braceCount > 0)
        {
            if (content[pos] == '{')
                braceCount++;
            else if (content[pos] == '}')
                braceCount--;
            pos++;
        }

        if (braceCount != 0)
            return false;

        block = content.Substring(startBrace + 1, pos - startBrace - 2);
        return true;
    }

    private bool TryExtractInlineBlock(string content, string key, out string block)
    {
        block = string.Empty;
        var match = Regex.Match(content, Regex.Escape(key) + @"\s*=\s*\{");
        if (!match.Success)
            return false;

        int startBrace = match.Index + match.Length - 1;
        int braceCount = 1;
        int pos = startBrace + 1;

        while (pos < content.Length && braceCount > 0)
        {
            if (content[pos] == '{')
                braceCount++;
            else if (content[pos] == '}')
                braceCount--;
            pos++;
        }

        if (braceCount != 0)
            return false;

        block = content.Substring(startBrace + 1, pos - startBrace - 2);
        return true;
    }

    private string? ExtractAdjectiveFromBlock(string block)
    {
        var matches = Regex.Matches(block, @"key=""([^""]+)""");
        if (matches.Count == 0)
            return null;

        var candidates = new List<string>();
        foreach (Match match in matches)
        {
            var value = match.Groups[1].Value;
            if (!value.StartsWith("%", StringComparison.Ordinal))
                candidates.Add(value);
        }

        if (candidates.Count == 0)
            return null;

        var species = candidates.FirstOrDefault(c => c.StartsWith("SPEC_", StringComparison.Ordinal));
        return species ?? candidates[0];
    }

    private string? ExtractNameFromBlock(string block)
    {
        var adjective = ExtractVariableValue(block, "adjective");
        var suffix = ExtractVariableValue(block, "1");

        if (!string.IsNullOrWhiteSpace(adjective) && !string.IsNullOrWhiteSpace(suffix))
            return $"{adjective} {suffix}";

        if (!string.IsNullOrWhiteSpace(adjective))
            return adjective;

        return suffix;
    }

    private string? ExtractVariableValue(string block, string variableKey)
    {
        var matches = Regex.Matches(block, @"key=""([^""]+)""");
        for (int i = 0; i < matches.Count - 1; i++)
        {
            if (!string.Equals(matches[i].Groups[1].Value, variableKey, StringComparison.Ordinal))
                continue;

            for (int j = i + 1; j < matches.Count; j++)
            {
                var value = matches[j].Groups[1].Value;
                if (!value.StartsWith("%", StringComparison.Ordinal))
                    return value;
            }
        }

        return null;
    }

    private string NormalizeLocalizationKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value.StartsWith("SPEC_", StringComparison.Ordinal))
            return value.Substring("SPEC_".Length).Replace('_', ' ');

        return value;
    }

    private string ToHumanLabel(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var normalized = value;
        if (!string.IsNullOrWhiteSpace(prefix) &&
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = value.Substring(prefix.Length);
        }

        normalized = normalized.Replace('_', ' ').Trim();
        if (normalized.Length == 0)
            return value;

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length == 1)
            {
                words[i] = word.ToUpperInvariant();
            }
            else
            {
                words[i] = char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
            }
        }

        return string.Join(' ', words);
    }

    private string ExtractEthos(string countryData)
    {
        if (!TryExtractInlineBlock(countryData, "ethos", out var ethosBlock))
            return string.Empty;

        var matches = Regex.Matches(ethosBlock, @"ethic=""([^""]+)""");
        if (matches.Count == 0)
            return string.Empty;

        var labels = matches
            .Select(m => m.Groups[1].Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => ToHumanLabel(v, "ethic_"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return labels.Count == 0 ? string.Empty : string.Join(", ", labels);
    }

    private string ExtractListValue(string countryData, string key, string prefix)
    {
        if (!TryExtractInlineBlock(countryData, key, out var block))
            return string.Empty;

        var values = Regex.Matches(block, "\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v =>
            {
                var raw = v;
                if (string.Equals(prefix, "civic_", StringComparison.OrdinalIgnoreCase) &&
                    raw.StartsWith("civic_hive_", StringComparison.OrdinalIgnoreCase))
                {
                    raw = "civic_" + raw.Substring("civic_hive_".Length);
                }
                return NormalizeCivicLabel(ToHumanLabel(raw, prefix));
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count == 0 ? string.Empty : string.Join(", ", values);
    }

    private string NormalizeCivicLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label;

        var trimmed = label.Trim();
        const string hivePrefix = "Hive ";
        while (trimmed.StartsWith(hivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(hivePrefix.Length).TrimStart();
        }

        return trimmed;
    }

    private int ExtractFederationId(string countryData)
    {
        var federationId = ExtractIntValue(countryData, @"federation=(\d+)");
        if (federationId == 0)
        {
            federationId = ExtractIntValue(countryData, @"associated_federation=(\d+)");
        }

        return federationId;
    }

    private string ExtractSubjectStatus(string countryData)
    {
        var overlordId = ExtractIntValue(countryData, @"overlord=(\d+)");
        if (overlordId != 0)
        {
            return $"Subject of: {overlordId}";
        }

        if (TryExtractInlineBlock(countryData, "subjects", out var subjectsBlock))
        {
            var subjects = ExtractIntList(subjectsBlock);
            if (subjects.Count > 0)
            {
                return $"Overlord of: {string.Join(", ", subjects)}";
            }
        }

        return string.Empty;
    }

    private string ExtractDiplomaticStance(string countryData)
    {
        var stance = ExtractStringValue(
            countryData,
            @"policy=""diplomatic_stance""[\s\S]{0,500}?selected=""([^""]+)""",
            @"policy=""diplomatic_stance""[\s\S]{0,500}?""(diplo_stance_[^""]+)"""
        );

        if (string.IsNullOrWhiteSpace(stance))
            return string.Empty;

        return ToHumanLabel(stance, "diplo_stance_");
    }

    private string ExtractDiplomaticWeight(string countryData)
    {
        var weight = ExtractDecimalValue(countryData, @"diplo_weight=([0-9.]+)");
        if (weight == 0m)
        {
            weight = ExtractDecimalValue(countryData, @"diplomatic_weight=([0-9.]+)");
        }

        return weight == 0m ? string.Empty : weight.ToString("0.##");
    }

    private Dictionary<int, FederationInfo> ParseFederationInfo(string content)
    {
        var result = new Dictionary<int, FederationInfo>();
        if (!TryExtractTopLevelBlock(content, "federation", out var federationContent))
            return result;

        foreach (var (federationId, federationData) in ExtractIdBlocks(federationContent))
        {
            var nameTemplate = ExtractNameTemplate(federationData);
            var typeFallback = ExtractStringValue(federationData, @"federation_type=""([^""]+)""");
            if (!string.IsNullOrWhiteSpace(typeFallback))
            {
                typeFallback = ToHumanLabel(typeFallback, "federation_");
            }
            var members = ExtractIntList(ExtractInlineBlockOrEmpty(federationData, "members"));
            if (!string.IsNullOrWhiteSpace(nameTemplate) || members.Count > 0)
            {
                result[federationId] = new FederationInfo(nameTemplate, typeFallback ?? string.Empty, members);
            }
        }

        return result;
    }

    private Dictionary<int, string> ResolveFederationNames(
        Dictionary<int, FederationInfo> federationInfo,
        Dictionary<int, string> countryAdjectiveById)
    {
        var result = new Dictionary<int, string>();
        foreach (var (id, info) in federationInfo)
        {
            var template = info.NameTemplate;
            if (string.IsNullOrWhiteSpace(template))
                continue;

            var adjective = info.Members
                .Select(memberId => countryAdjectiveById.TryGetValue(memberId, out var adj) ? adj : null)
                .FirstOrDefault(adj => !string.IsNullOrWhiteSpace(adj));

            var resolved = template;
            if (!string.IsNullOrWhiteSpace(adjective) && resolved.Contains("%ADJ%", StringComparison.OrdinalIgnoreCase))
            {
                resolved = resolved.Replace("%ADJ%", adjective, StringComparison.OrdinalIgnoreCase);
            }

            if (resolved.Contains("%ADJ%", StringComparison.OrdinalIgnoreCase))
            {
                resolved = string.Empty;
            }

            resolved = NormalizeFederationName(resolved);
            if (string.IsNullOrWhiteSpace(resolved) && !string.IsNullOrWhiteSpace(info.TypeFallback))
            {
                resolved = info.TypeFallback;
            }

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                result[id] = resolved;
            }
        }

        return result;
    }

    private string ExtractNameTemplate(string federationData)
    {
        if (!TryExtractInlineBlock(federationData, "name", out var nameBlock))
            return string.Empty;

        var keys = Regex.Matches(nameBlock, @"key=""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .Where(v => !string.IsNullOrWhiteSpace(v) && !Regex.IsMatch(v, @"^\d+$"))
            .ToList();

        if (keys.Count == 0)
            return string.Empty;

        return string.Join(' ', keys);
    }

    private string ExtractInlineBlockOrEmpty(string content, string key)
    {
        return TryExtractInlineBlock(content, key, out var block) ? block : string.Empty;
    }

    private record FederationInfo(string NameTemplate, string TypeFallback, List<int> Members);

    private string NormalizeFederationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value.StartsWith("NAME_", StringComparison.Ordinal))
        {
            return value.Substring("NAME_".Length).Replace('_', ' ');
        }

        return value.Replace('_', ' ');
    }

    private List<ParsedWar> ParseWars(
        string content,
        string gameDate,
        Dictionary<int, string> countryNameById,
        Dictionary<int, string> countryAdjectiveById)
    {
        var result = new List<ParsedWar>();
        if (!TryExtractTopLevelBlock(content, "war", out var warsContent))
            return result;

        foreach (var (warId, warData) in ExtractIdBlocks(warsContent))
        {
            var attackers = ExtractWarSideCountries(warData, "attackers");
            var defenders = ExtractWarSideCountries(warData, "defenders");
            if (attackers.Count == 0 && defenders.Count == 0)
                continue;

            var attackerExhaustion = ExtractLastDecimal(warData, @"attacker_war_exhaustion=([0-9.]+)");
            var defenderExhaustion = ExtractLastDecimal(warData, @"defender_war_exhaustion=([0-9.]+)");
            var startDate = ExtractStringValue(warData, @"start_date=\s*""([^""]+)""") ?? string.Empty;

            var attackerLabel = BuildWarSideLabel(attackers, countryNameById);
            var defenderLabel = BuildWarSideLabel(defenders, countryNameById);

            var warName = BuildWarName(attackerLabel, defenderLabel);

            var warLength = BuildWarLength(gameDate, startDate);

            result.Add(new ParsedWar(
                warId,
                attackerLabel,
                defenderLabel,
                warName,
                startDate,
                warLength,
                attackerExhaustion,
                defenderExhaustion,
                attackers,
                defenders
            ));
        }

        return result;
    }

    private List<int> ExtractWarSideCountries(string warData, string key)
    {
        if (!TryExtractInlineBlock(warData, key, out var block))
            return new List<int>();

        var matches = Regex.Matches(block, @"country=(\d+)");
        return matches
            .Select(m => int.TryParse(m.Groups[1].Value, out var id) ? id : 0)
            .Where(id => id != 0)
            .Distinct()
            .ToList();
    }

    private decimal ExtractLastDecimal(string content, string pattern)
    {
        var matches = Regex.Matches(content, pattern, RegexOptions.Multiline);
        if (matches.Count == 0)
            return 0m;

        var value = matches[^1].Groups[1].Value;
        return decimal.TryParse(value, out var parsed) ? parsed : 0m;
    }

    private string BuildWarSideLabel(List<int> ids, Dictionary<int, string> namesById)
    {
        if (ids.Count == 0)
            return string.Empty;

        var names = ids
            .Select(id => namesById.TryGetValue(id, out var name) ? name : $"Country {id}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(", ", names);
    }

    private string BuildWarName(string attacker, string defender)
    {
        if (string.IsNullOrWhiteSpace(attacker) || string.IsNullOrWhiteSpace(defender))
            return string.Empty;

        return $"{attacker} vs {defender}";
    }

    private string BuildWarLength(string currentDate, string startDate)
    {
        if (!TryParseGameDate(currentDate, out var current) || !TryParseGameDate(startDate, out var start))
            return string.Empty;

        var months = (current.Year - start.Year) * 12 + (current.Month - start.Month);
        if (current.Day < start.Day)
            months -= 1;
        if (months < 0)
            months = 0;

        var years = months / 12;
        var remMonths = months % 12;
        return years > 0 ? $"{years}y {remMonths}m" : $"{remMonths}m";
    }

    private bool TryParseGameDate(string value, out DateTime date)
    {
        date = default;
        var match = Regex.Match(value, @"^(\d{4})\.(\d{2})\.(\d{2})$");
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups[1].Value, out var year) ||
            !int.TryParse(match.Groups[2].Value, out var month) ||
            !int.TryParse(match.Groups[3].Value, out var day))
        {
            return false;
        }

        try
        {
            date = new DateTime(year, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private bool ShouldExcludeByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.Equals("global_event_country", StringComparison.OrdinalIgnoreCase)
            || name.Equals("name_animator", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsExcludedCountry(string countryData)
    {
        if (TryExtractInlineBlock(countryData, "ai", out var aiBlock))
        {
            var match = Regex.Match(aiBlock, @"\btype=""([^""]+)""");
            if (match.Success)
            {
                var aiType = match.Groups[1].Value;
                return aiType is "global_event" or "shroud" or "shroud_spirits" or "enclave" or "mercenary";
            }
        }

        if (Regex.IsMatch(countryData, @"\btype=""primitive""", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(countryData, @"\bpersonality=""pre_ftl_", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(countryData, @"\borigin=""origin_default_pre_ftl""", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(countryData, @"\bcategory=""pre_ftl""", RegexOptions.IgnoreCase))
        {
            return true;
        }

        return false;
    }

    private List<BudgetLineItem> ParseBudget(int countryId, string countryData)
    {
        var items = new List<BudgetLineItem>();
        
        // Find budget section - looking for "current_month=" and extract from there
        int budgetStart = countryData.IndexOf("budget=", StringComparison.Ordinal);
        if (budgetStart == -1)
            return items;
        
        // Find the opening brace of budget
        int openBrace = countryData.IndexOf('{', budgetStart);
        if (openBrace == -1)
            return items;
        
        // Find current_month section
        int currentMonthStart = countryData.IndexOf("current_month=", openBrace, StringComparison.Ordinal);
        if (currentMonthStart == -1)
            return items;
        
        // Find the opening brace of current_month
        int currentMonthBrace = countryData.IndexOf('{', currentMonthStart);
        if (currentMonthBrace == -1)
            return items;
        
        // Extract content until we find the matching closing brace
        int braceCount = 1;
        int pos = currentMonthBrace + 1;
        while (pos < countryData.Length && braceCount > 0)
        {
            if (countryData[pos] == '{')
                braceCount++;
            else if (countryData[pos] == '}')
                braceCount--;
            pos++;
        }
        
        string currentMonthContent = countryData.Substring(currentMonthBrace + 1, pos - currentMonthBrace - 2);
        
        // Parse income, expenses, balance sections
        var sections = new[] { "income", "expenses", "balance" };
        
        foreach (var section in sections)
        {
            int sectionStart = currentMonthContent.IndexOf(section + "=", StringComparison.Ordinal);
            if (sectionStart == -1)
                continue;
            
            int sectionBrace = currentMonthContent.IndexOf('{', sectionStart);
            if (sectionBrace == -1)
                continue;
            
            // Extract the section content
            braceCount = 1;
            pos = sectionBrace + 1;
            while (pos < currentMonthContent.Length && braceCount > 0)
            {
                if (currentMonthContent[pos] == '{')
                    braceCount++;
                else if (currentMonthContent[pos] == '}')
                    braceCount--;
                pos++;
            }
            
            string sectionContent = currentMonthContent.Substring(sectionBrace + 1, pos - sectionBrace - 2);
            var categoryItems = ExtractBudgetCategories(section, countryId, sectionContent);
            items.AddRange(categoryItems);
        }
        
        return items;
    }

    private List<BudgetLineItem> ExtractBudgetCategories(string section, int countryId, string content)
    {
        var items = new List<BudgetLineItem>();
        
        // Match patterns like: category_name={ resource1=value1 resource2=value2 }
        var categoryPattern = @"(\w+)=\s*\{(.*?)\}";
        
        foreach (Match categoryMatch in Regex.Matches(content, categoryPattern, RegexOptions.Singleline))
        {
            var category = categoryMatch.Groups[1].Value;
            var categoryContent = categoryMatch.Groups[2].Value;
            
            // Extract individual resource amounts: resource=value
            var resourcePattern = @"(\w+)=([0-9.]+)";
            foreach (Match resourceMatch in Regex.Matches(categoryContent, resourcePattern))
            {
                var resourceType = resourceMatch.Groups[1].Value;
                if (decimal.TryParse(resourceMatch.Groups[2].Value, out var amount))
                {
                    items.Add(new BudgetLineItem
                    {
                        CountryId = countryId,
                        Section = section,
                        Category = category,
                        ResourceType = resourceType,
                        Amount = amount
                    });
                }
            }
        }
        
        return items;
    }

    private List<SpeciesPopulation> ParseSpeciesDemographics(string content, HashSet<int> allowedCountries)
    {
        if (!TryExtractTopLevelBlock(content, "species_db", out var speciesDbContent))
        {
            return new List<SpeciesPopulation>();
        }

        if (!TryExtractTopLevelBlock(content, "planets", out var planetsContent))
        {
            return new List<SpeciesPopulation>();
        }

        if (!TryExtractTopLevelBlock(content, "pop_groups", out var popGroupsContent))
        {
            return new List<SpeciesPopulation>();
        }

        var speciesNames = new Dictionary<int, string>();
        foreach (var (speciesId, speciesData) in ExtractIdBlocks(speciesDbContent))
        {
            var name = ExtractStringValue(
                speciesData,
                @"name=\s*\{\s*key=""([^""]+)""",
                @"name=""([^""]+)""");
            if (string.IsNullOrWhiteSpace(name))
                continue;
            speciesNames[speciesId] = NormalizeLocalizationKey(name);
        }

        var planetOwners = new Dictionary<int, int>();
        if (TryExtractInlineBlock(planetsContent, "planet", out var planetContent))
        {
            foreach (var (planetId, planetData) in ExtractIdBlocks(planetContent))
            {
                var owner = ExtractIntValue(planetData, @"owner=(\d+)");
                if (owner == 0)
                {
                    owner = ExtractIntValue(planetData, @"controller=(\d+)");
                }

                if (owner != 0)
                {
                    planetOwners[planetId] = owner;
                }
            }
        }

        if (TryExtractTopLevelBlock(content, "country", out var countriesContent))
        {
            foreach (var (countryId, countryData) in ExtractCountryBlocks(countriesContent))
            {
                if (!allowedCountries.Contains(countryId))
                    continue;

                if (TryExtractInlineBlock(countryData, "owned_planets", out var ownedPlanets))
                {
                    foreach (var planetId in ExtractIntList(ownedPlanets))
                    {
                        if (!planetOwners.ContainsKey(planetId))
                        {
                            planetOwners[planetId] = countryId;
                        }
                    }
                }

                if (TryExtractInlineBlock(countryData, "controlled_planets", out var controlledPlanets))
                {
                    foreach (var planetId in ExtractIntList(controlledPlanets))
                    {
                        if (!planetOwners.ContainsKey(planetId))
                        {
                            planetOwners[planetId] = countryId;
                        }
                    }
                }
            }
        }

        var totalsByCountry = new Dictionary<int, Dictionary<int, decimal>>();

        foreach (var (_, popGroupData) in ExtractIdBlocks(popGroupsContent))
        {
            var planetId = ExtractIntValue(popGroupData, @"planet=(\d+)");
            if (planetId == 0 || !planetOwners.TryGetValue(planetId, out var ownerId))
                continue;
            if (!allowedCountries.Contains(ownerId))
                continue;

            var speciesId = ExtractIntValue(popGroupData, @"species=(\d+)");
            if (speciesId == 0)
                continue;

            var size = ExtractDecimalValue(popGroupData, @"size=([0-9.]+)");
            if (size <= 0)
                continue;

            if (!totalsByCountry.TryGetValue(ownerId, out var speciesTotals))
            {
                speciesTotals = new Dictionary<int, decimal>();
                totalsByCountry[ownerId] = speciesTotals;
            }

            speciesTotals[speciesId] = speciesTotals.GetValueOrDefault(speciesId) + size;
        }

        var results = new List<SpeciesPopulation>();
        foreach (var (countryId, speciesTotals) in totalsByCountry)
        {
            foreach (var (speciesId, amount) in speciesTotals)
            {
                speciesNames.TryGetValue(speciesId, out var name);
                results.Add(new SpeciesPopulation
                {
                    CountryId = countryId,
                    SpeciesId = speciesId,
                    SpeciesName = string.IsNullOrWhiteSpace(name) ? $"Species {speciesId}" : name!,
                    Amount = amount
                });
            }
        }

        return results;
    }

    private List<GlobalSpeciesPopulation> ParseGlobalSpeciesDemographics(string content)
    {
        if (!TryExtractTopLevelBlock(content, "species_db", out var speciesDbContent))
        {
            return new List<GlobalSpeciesPopulation>();
        }

        if (!TryExtractTopLevelBlock(content, "pop_groups", out var popGroupsContent))
        {
            return new List<GlobalSpeciesPopulation>();
        }

        var speciesNames = new Dictionary<int, string>();
        foreach (var (speciesId, speciesData) in ExtractIdBlocks(speciesDbContent))
        {
            var name = ExtractStringValue(
                speciesData,
                @"name=\s*\{\s*key=""([^""]+)""",
                @"name=""([^""]+)""");
            if (string.IsNullOrWhiteSpace(name))
                continue;
            speciesNames[speciesId] = NormalizeLocalizationKey(name);
        }

        var totals = new Dictionary<int, decimal>();
        foreach (var (_, popGroupData) in ExtractIdBlocks(popGroupsContent))
        {
            var speciesId = ExtractIntValue(popGroupData, @"species=(\d+)");
            if (speciesId == 0)
                continue;

            var size = ExtractDecimalValue(popGroupData, @"size=([0-9.]+)");
            if (size <= 0)
                continue;

            totals[speciesId] = totals.GetValueOrDefault(speciesId) + size;
        }

        var results = new List<GlobalSpeciesPopulation>();
        foreach (var (speciesId, amount) in totals)
        {
            speciesNames.TryGetValue(speciesId, out var name);
            results.Add(new GlobalSpeciesPopulation
            {
                SpeciesId = speciesId,
                SpeciesName = string.IsNullOrWhiteSpace(name) ? $"Species {speciesId}" : name!,
                Amount = amount
            });
        }

        return results;
    }

    private string? ExtractStringValue(string content, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(content, pattern, RegexOptions.Multiline);
            if (match.Success)
                return match.Groups[1].Value;
        }
        return null;
    }

    private List<int> ExtractIntList(string content)
    {
        var result = new List<int>();
        foreach (Match match in Regex.Matches(content, @"\b(\d+)\b"))
        {
            if (int.TryParse(match.Groups[1].Value, out var value))
            {
                result.Add(value);
            }
        }
        return result;
    }

    private int ExtractIntValue(string content, string pattern)
    {
        var match = Regex.Match(content, pattern, RegexOptions.Multiline);
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : 0;
    }

    private long ExtractLongValue(string content, string pattern)
    {
        var match = Regex.Match(content, pattern, RegexOptions.Multiline);
        return match.Success && long.TryParse(match.Groups[1].Value, out var value) ? value : 0;
    }

    private decimal ExtractDecimalValue(string content, string pattern)
    {
        var match = Regex.Match(content, pattern, RegexOptions.Multiline);
        return match.Success && decimal.TryParse(match.Groups[1].Value, out var value) ? value : 0m;
    }
}

public record ParsedWar(
    int WarId,
    string Attackers,
    string Defenders,
    string WarName,
    string WarStartDate,
    string WarLength,
    decimal AttackerWarExhaustion,
    decimal DefenderWarExhaustion,
    List<int> AttackerIds,
    List<int> DefenderIds);

public class ParseResult
{
    public List<Country> Countries { get; set; } = new();
    public List<BudgetLineItem> BudgetLineItems { get; set; } = new();
    public List<SpeciesPopulation> SpeciesPopulations { get; set; } = new();
    public List<GlobalSpeciesPopulation> GlobalSpeciesPopulations { get; set; } = new();
    public List<ParsedWar> Wars { get; set; } = new();
    public string GameDate { get; set; } = "Unknown";
    public int Tick { get; set; }
}
