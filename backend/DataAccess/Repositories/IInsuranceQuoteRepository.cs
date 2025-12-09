using backend.DataAccess.Models;

namespace backend.DataAccess.Repositories;

public interface IInsuranceQuoteRepository
{
    Task<InsuranceQuote> CreateAsync(InsuranceQuote quote);
    Task<InsuranceQuote?> GetByIdAsync(string id);
    Task<List<InsuranceQuote>> GetAllAsync();
    Task<List<InsuranceQuote>> GetByLocationAsync(string location);
}
