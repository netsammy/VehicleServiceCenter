using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data;

public interface IServiceRepository
{
    Task<List<ServiceRecord>> ListAsync(int? limit = null);
    Task<ServiceRecord?> GetAsync(int id);
    Task<List<ServiceItem>> ListItemsAsync(int serviceRecordId);
    Task<int> SaveAsync(ServiceRecord record);
    Task<int> DeleteAsync(ServiceRecord record);
    Task DropTableAsync();
    Task<string> GenerateReceiptNumberAsync();
}
