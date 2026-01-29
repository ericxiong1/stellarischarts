namespace StellarisCharts.Api.Models;

public class WarStatus
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public int WarId { get; set; }
    public string WarName { get; set; } = string.Empty;
    public string WarStartDate { get; set; } = string.Empty;
    public string WarLength { get; set; } = string.Empty;
    public decimal AttackerWarExhaustion { get; set; }
    public decimal DefenderWarExhaustion { get; set; }
    public string Attackers { get; set; } = string.Empty;
    public string Defenders { get; set; } = string.Empty;
}
