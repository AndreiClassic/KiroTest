namespace backend.Services;

public interface IPdfService
{
    byte[] GeneratePropertyReport(int lotId, Models.Lot.LotData lotData, Models.InsuranceResponse? insuranceQuote);
}
