namespace StellarisCharts.Api.Models;

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Adjective { get; set; } = null!;
    public int CountryId { get; set; }
    public string GovernmentType { get; set; } = null!;
    public string Authority { get; set; } = null!;
    public string Ethos { get; set; } = string.Empty;
    public string Civics { get; set; } = string.Empty;
    public string TraditionTrees { get; set; } = string.Empty;
    public string AscensionPerks { get; set; } = string.Empty;
    public string FederationType { get; set; } = string.Empty;
    public string SubjectStatus { get; set; } = string.Empty;
    public string DiplomaticStance { get; set; } = string.Empty;
    public string DiplomaticWeight { get; set; } = string.Empty;
    public string Personality { get; set; } = null!;
    public string GraphicalCulture { get; set; } = null!;
    public int Capital { get; set; }
    public decimal MilitaryPower { get; set; }
    public decimal EconomyPower { get; set; }
    public decimal TechPower { get; set; }
    public int FleetSize { get; set; }
    public int EmpireSize { get; set; }
    public long NumSapientPops { get; set; }
    public int VictoryRank { get; set; }
    public decimal VictoryScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Snapshot> Snapshots { get; set; } = [];
}
