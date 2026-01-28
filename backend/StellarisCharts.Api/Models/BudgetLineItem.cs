namespace StellarisCharts.Api.Models;

public class BudgetLineItem
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public int CountryId { get; set; }
    public string Section { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public decimal Amount { get; set; }

    public Snapshot? Snapshot { get; set; }
    public Country? Country { get; set; }
}
