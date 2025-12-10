using DataAccess.Models;

namespace DataAccess.Repositories;

public interface IFloodQueryRepository
{
    Task<FloodQuery?> GetCachedQueryAsync(decimal latitude, decimal longitude);
    Task<FloodQuery?> GetByParcelIdAsync(string parcelId);
    Task SaveQueryAsync(FloodQuery query);
    Task<List<FloodQuery>> GetRecentQueriesAsync(int limit = 100);
}
