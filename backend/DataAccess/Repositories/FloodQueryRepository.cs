using backend.DataAccess.Configuration;
using DataAccess.Models;
using DataAccess.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace backend.DataAccess.Repositories;

public class FloodQueryRepository : IFloodQueryRepository
{
    private readonly IMongoCollection<FloodQuery> _floodQueries;
    
    public FloodQueryRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _floodQueries = database.GetCollection<FloodQuery>("flood_queries");
        
        // Create indexes for efficient querying
        var latLongIndex = Builders<FloodQuery>.IndexKeys
            .Ascending(q => q.Latitude)
            .Ascending(q => q.Longitude);
        _floodQueries.Indexes.CreateOne(new CreateIndexModel<FloodQuery>(latLongIndex));
        
        var parcelIdIndex = Builders<FloodQuery>.IndexKeys.Ascending(q => q.ParcelId);
        _floodQueries.Indexes.CreateOne(new CreateIndexModel<FloodQuery>(parcelIdIndex));
    }
    
    public async Task<FloodQuery?> GetCachedQueryAsync(decimal latitude, decimal longitude)
    {
        // Find cached query within ~100m radius (approximately 0.001 degrees)
        var tolerance = 0.001m;
        
        var filter = Builders<FloodQuery>.Filter.And(
            Builders<FloodQuery>.Filter.Gte(q => q.Latitude, latitude - tolerance),
            Builders<FloodQuery>.Filter.Lte(q => q.Latitude, latitude + tolerance),
            Builders<FloodQuery>.Filter.Gte(q => q.Longitude, longitude - tolerance),
            Builders<FloodQuery>.Filter.Lte(q => q.Longitude, longitude + tolerance),
            Builders<FloodQuery>.Filter.Or(
                Builders<FloodQuery>.Filter.Eq(q => q.ExpiresAt, null),
                Builders<FloodQuery>.Filter.Gt(q => q.ExpiresAt, DateTime.UtcNow)
            )
        );
        
        return await _floodQueries.Find(filter)
            .SortByDescending(q => q.QueriedAt)
            .FirstOrDefaultAsync();
    }
    
    public async Task<FloodQuery?> GetByParcelIdAsync(string parcelId)
    {
        var filter = Builders<FloodQuery>.Filter.And(
            Builders<FloodQuery>.Filter.Eq(q => q.ParcelId, parcelId),
            Builders<FloodQuery>.Filter.Or(
                Builders<FloodQuery>.Filter.Eq(q => q.ExpiresAt, null),
                Builders<FloodQuery>.Filter.Gt(q => q.ExpiresAt, DateTime.UtcNow)
            )
        );
        
        return await _floodQueries.Find(filter)
            .SortByDescending(q => q.QueriedAt)
            .FirstOrDefaultAsync();
    }
    
    public async Task SaveQueryAsync(FloodQuery query)
    {
        await _floodQueries.InsertOneAsync(query);
    }
    
    public async Task<List<FloodQuery>> GetRecentQueriesAsync(int limit = 100)
    {
        return await _floodQueries.Find(_ => true)
            .SortByDescending(q => q.QueriedAt)
            .Limit(limit)
            .ToListAsync();
    }
}
