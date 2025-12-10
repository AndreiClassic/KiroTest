using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DataAccess.Repositories;
using backend.DataAccess.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsuranceController : ControllerBase
{
    private readonly IInsuranceQuoteRepository _repository;
    private readonly ILogger<InsuranceController> _logger;

    public InsuranceController(IInsuranceQuoteRepository repository, ILogger<InsuranceController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<InsuranceResponse>> Calculate([FromBody] InsuranceRequest request)
    {
        _logger.LogInformation("Calculating insurance for house value: {HouseValue}, Location: {Location}", 
            request.HouseValue, request.Location);
        
        // Simple calculation logic for NZ house insurance
        decimal baseRate = 0.003m; // 0.3% of house value
        
        // Age factor
        int age = DateTime.Now.Year - request.BuildYear;
        decimal ageFactor = age > 50 ? 1.3m : age > 30 ? 1.15m : 1.0m;
        
        // Construction type factor
        decimal constructionFactor = request.ConstructionType.ToLower() switch
        {
            "brick" => 0.9m,
            "weatherboard" => 1.1m,
            "concrete" => 0.85m,
            _ => 1.0m
        };
        
        // Location risk (simplified)
        decimal locationFactor = request.Location.ToLower() switch
        {
            "auckland" => 1.1m,
            "wellington" => 1.2m,
            "christchurch" => 1.15m,
            _ => 1.0m
        };
        
        // Flood zone factor
        decimal floodFactor = (request.FloodZone?.ToLower()) switch
        {
            "high" => 1.3m,
            "medium" => 1.15m,
            "low" => 1.0m,
            _ => 1.0m
        };
        
        // Earthquake zone factor
        decimal earthquakeFactor = (request.EarthquakeZone?.ToLower()) switch
        {
            "high" => 1.25m,
            "medium" => 1.1m,
            "low" => 1.0m,
            _ => 1.0m
        };
        
        decimal annualPremium = request.HouseValue * baseRate * ageFactor * constructionFactor * locationFactor * floodFactor * earthquakeFactor;
        
        _logger.LogInformation("Premium factors - Age: {Age}, Construction: {Construction}, Location: {Location}, Flood: {Flood}, Earthquake: {Earthquake}", 
            ageFactor, constructionFactor, locationFactor, floodFactor, earthquakeFactor);
        
        string riskLevel = annualPremium / request.HouseValue > 0.004m ? "High" : 
                          annualPremium / request.HouseValue > 0.003m ? "Medium" : "Low";
        
        var response = new InsuranceResponse
        {
            AnnualPremium = Math.Round(annualPremium, 2),
            MonthlyPremium = Math.Round(annualPremium / 12, 2),
            RiskLevel = riskLevel
        };

        // Save quote to database
        var quote = new InsuranceQuote
        {
            HouseValue = request.HouseValue,
            BuildYear = request.BuildYear,
            Location = request.Location,
            ConstructionType = request.ConstructionType,
            Bedrooms = request.Bedrooms,
            AnnualPremium = response.AnnualPremium,
            MonthlyPremium = response.MonthlyPremium,
            RiskLevel = response.RiskLevel
        };

        await _repository.CreateAsync(quote);
        
        _logger.LogInformation("Insurance quote saved. Annual Premium: {AnnualPremium}, Risk: {RiskLevel}", 
            response.AnnualPremium, response.RiskLevel);
        
        return Ok(response);
    }
}
