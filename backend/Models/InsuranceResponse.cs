namespace backend.Models;

public class InsuranceResponse
{
    public decimal AnnualPremium { get; set; }
    public decimal MonthlyPremium { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
