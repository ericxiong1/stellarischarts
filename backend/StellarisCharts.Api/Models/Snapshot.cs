namespace StellarisCharts.Api.Models;

public class Snapshot
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string GameDate { get; set; } = null!;
    public int Tick { get; set; }
    public decimal MilitaryPower { get; set; }
    public decimal EconomyPower { get; set; }
    public decimal TechPower { get; set; }
    public int FleetSize { get; set; }
    public int EmpireSize { get; set; }
    public long NumSapientPops { get; set; }
    public int VictoryRank { get; set; }
    public decimal VictoryScore { get; set; }
    public DateTime SnapshotTime { get; set; }

    public Country? Country { get; set; }
    public ICollection<BudgetLineItem> BudgetLineItems { get; set; } = [];
}
