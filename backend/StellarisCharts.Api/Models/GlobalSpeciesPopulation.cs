namespace StellarisCharts.Api.Models;

public class GlobalSpeciesPopulation
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public int SpeciesId { get; set; }
    public string SpeciesName { get; set; } = null!;
    public decimal Amount { get; set; }

    public Snapshot? Snapshot { get; set; }
}
