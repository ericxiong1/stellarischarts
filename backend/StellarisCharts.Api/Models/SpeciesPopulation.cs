namespace StellarisCharts.Api.Models;

public class SpeciesPopulation
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public int CountryId { get; set; }
    public int SpeciesId { get; set; }
    public string SpeciesName { get; set; } = null!;
    public decimal Amount { get; set; }

    public Snapshot? Snapshot { get; set; }
    public Country? Country { get; set; }
}
