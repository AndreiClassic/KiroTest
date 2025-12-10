using backend.Models.Hazard;

namespace backend.Services;

public interface IHazardService
{
    Task<HazardInfo> GetHazardInfoAsync(string address);
    Task<GeocodingResult?> GeocodeAddressAsync(string address);
}
