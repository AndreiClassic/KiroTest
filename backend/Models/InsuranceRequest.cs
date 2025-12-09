namespace backend.Models;

public class InsuranceRequest
{
    public decimal HouseValue { get; set; }
    public int BuildYear { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
}
