using backend.Models.Lot;

namespace backend.Services;

public interface ILotService
{
    Task<LotData?> GetLotDataAsync(int lotId);
}
