namespace StellarisCharts.Api.Models;

public class ResourceStockpile
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public int CountryId { get; set; }
    public string ResourceType { get; set; } = null!;
    public decimal Amount { get; set; }

    public Snapshot? Snapshot { get; set; }
    public Country? Country { get; set; }
}
