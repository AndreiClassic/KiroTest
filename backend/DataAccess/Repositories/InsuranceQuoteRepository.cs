using backend.DataAccess.Configuration;
using backend.DataAccess.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace backend.DataAccess.Repositories;

public class InsuranceQuoteRepository : IInsuranceQuoteRepository
{
    private readonly IMongoCollection<InsuranceQuote> _quotes;

    public InsuranceQuoteRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _quotes = database.GetCollection<InsuranceQuote>("quotes");
    }

    public async Task<InsuranceQuote> CreateAsync(InsuranceQuote quote)
    {
        await _quotes.InsertOneAsync(quote);
        return quote;
    }

    public async Task<InsuranceQuote?> GetByIdAsync(string id)
    {
        return await _quotes.Find(q => q.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<InsuranceQuote>> GetAllAsync()
    {
        return await _quotes.Find(_ => true).ToListAsync();
    }

    public async Task<List<InsuranceQuote>> GetByLocationAsync(string location)
    {
        return await _quotes.Find(q => q.Location == location).ToListAsync();
    }
}
