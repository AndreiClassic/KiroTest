using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccess.Models;

public class FloodQuery
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ParcelId { get; set; }
    public string FloodZone { get; set; } = "Unknown";
    public string Region { get; set; } = string.Empty;
    public string? FloodCategory { get; set; }
    public decimal? ReturnPeriod { get; set; } // e.g., 100-year flood
    public DateTime QueriedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } // Cache expiration
}
