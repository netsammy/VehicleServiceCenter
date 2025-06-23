using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    
    // New methods
    Task<bool> AddUserAsync(string username, string password);
    Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword);
    Task<List<string>> GetAllUsernamesAsync();
}
