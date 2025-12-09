using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.DataAccess.Models;

public class InsuranceQuote
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public decimal HouseValue { get; set; }
    public int BuildYear { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    
    public decimal AnnualPremium { get; set; }
    public decimal MonthlyPremium { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
