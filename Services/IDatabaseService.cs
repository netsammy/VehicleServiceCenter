using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task<bool> TestConnectionAsync();
    Task<User?> GetUserAsync(string username);
    Task<List<string>> GetAllUsernamesAsync();
    Task<bool> AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string username);
    Task<bool> VerifyUserCredentialsAsync(string username, string passwordHash);
}
