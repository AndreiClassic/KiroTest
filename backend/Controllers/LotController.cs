using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using backend.Models.Lot;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotController : ControllerBase
{
    private readonly ILotService _lotService;
    private readonly ILogger<LotController> _logger;

    public LotController(ILotService lotService, ILogger<LotController> logger)
    {
        _lotService = lotService;
        _logger = logger;
    }

    [HttpGet("{lotId}")]
    public async Task<ActionResult<LotData>> GetLot(int lotId)
    {
        _logger.LogInformation("Fetching lot data for Lot ID: {LotId}", lotId);
        
        var lotData = await _lotService.GetLotDataAsync(lotId);
        
        if (lotData == null)
        {
            _logger.LogWarning("Lot {LotId} not found or external service unavailable", lotId);
            return NotFound(new { message = $"Lot {lotId} not found or service unavailable" });
        }

        _logger.LogInformation("Lot {LotId} data retrieved successfully. Address: {Address}", 
            lotId, lotData.Address);
        
        return Ok(lotData);
    }

    [HttpGet("{lotId}/insurance-estimate")]
    public async Task<ActionResult<InsuranceResponse>> GetInsuranceEstimate(int lotId)
    {
        var lotData = await _lotService.GetLotDataAsync(lotId);
        
        if (lotData == null)
        {
            return NotFound(new { message = $"Lot {lotId} not found or service unavailable" });
        }

        // Estimate house value based on floor area (rough NZ average: $3,500 per sqm)
        decimal estimatedHouseValue = lotData.FloorArea * 3500;

        // Use current year if build year not available
        int buildYear = lotData.BuildYear ?? DateTime.Now.Year;

        // Simple calculation
        decimal baseRate = 0.003m;
        int age = DateTime.Now.Year - buildYear;
        decimal ageFactor = age > 50 ? 1.3m : age > 30 ? 1.15m : 1.0m;
        
        // Map location to known NZ cities
        decimal locationFactor = lotData.Location.ToLower() switch
        {
            var loc when loc.Contains("auckland") => 1.1m,
            var loc when loc.Contains("wellington") => 1.2m,
            var loc when loc.Contains("christchurch") => 1.15m,
            _ => 1.0m
        };

        decimal annualPremium = estimatedHouseValue * baseRate * ageFactor * locationFactor;
        
        string riskLevel = annualPremium / estimatedHouseValue > 0.004m ? "High" : 
                          annualPremium / estimatedHouseValue > 0.003m ? "Medium" : "Low";

        return Ok(new
        {
            lotId,
            address = lotData.Address,
            location = lotData.Location,
            estimatedHouseValue,
            floorArea = lotData.FloorArea,
            bedrooms = lotData.Bedrooms,
            buildYear,
            annualPremium = Math.Round(annualPremium, 2),
            monthlyPremium = Math.Round(annualPremium / 12, 2),
            riskLevel
        });
    }
}
