using System.Security.Cryptography;
using System.Text;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Services;

public class AuthService : IAuthService
{
    private const string UserKey = "CurrentUser";
    private const string IsAuthenticatedKey = "IsAuthenticated";
    
    private readonly IPreferences _preferences;
    private readonly IDatabaseService _databaseService;
    
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser?.IsAuthenticated ?? false;

    public AuthService(IPreferences preferences, IDatabaseService databaseService)
    {
        _preferences = preferences;
        _databaseService = databaseService;
        
        // Initialize database and create Users table if it doesn't exist
        Task.Run(async () => await _databaseService.InitializeDatabaseAsync()).Wait();
        
        LoadUserState();
    }
    
    private void LoadUserState()
    {
        var isAuthenticated = _preferences.Get(IsAuthenticatedKey, false);
        if (isAuthenticated)
        {
            var username = _preferences.Get(UserKey, string.Empty);
            if (!string.IsNullOrEmpty(username))
            {
                // Get user from database
                var user = Task.Run(async () => await _databaseService.GetUserAsync(username)).Result;
                if (user != null)
                {
                    user.IsAuthenticated = true;
                    CurrentUser = user;
                }
            }
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }
        
        // Hash the password
        var passwordHash = HashPassword(password);
        
        // Verify credentials against database
        var isValid = await _databaseService.VerifyUserCredentialsAsync(username, passwordHash);
        if (isValid)
        {
            var user = await _databaseService.GetUserAsync(username);
            if (user != null)
            {
                user.IsAuthenticated = true;
                CurrentUser = user;
                SaveUserState();
                return true;
            }
        }

        return false;
    }    public async Task LogoutAsync()
    {
        await Task.Yield(); // Make method truly async
        
        if (CurrentUser != null)
        {
            CurrentUser.IsAuthenticated = false;
        }
        
        CurrentUser = null;
        _preferences.Remove(UserKey);
        _preferences.Set(IsAuthenticatedKey, false);
    }
    
    public async Task<bool> AddUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }
        
        // Create new user
        var newUser = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            IsAuthenticated = false
        };
        
        // Add user to database
        return await _databaseService.AddUserAsync(newUser);
    }
    
    public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(currentPassword) || 
            string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }
        
        // Hash passwords
        var currentPasswordHash = HashPassword(currentPassword);
        var newPasswordHash = HashPassword(newPassword);
        
        // Verify current password
        var isValid = await _databaseService.VerifyUserCredentialsAsync(username, currentPasswordHash);
        if (!isValid)
        {
            return false;
        }
        
        // Get user
        var user = await _databaseService.GetUserAsync(username);
        if (user == null)
        {
            return false;
        }
        
        // Update password
        user.PasswordHash = newPasswordHash;
        return await _databaseService.UpdateUserAsync(user);
    }
    
    public async Task<List<string>> GetAllUsernamesAsync()
    {
        return await _databaseService.GetAllUsernamesAsync();
    }

    private void SaveUserState()
    {
        if (CurrentUser != null)
        {
            _preferences.Set(UserKey, CurrentUser.Username);
            _preferences.Set(IsAuthenticatedKey, CurrentUser.IsAuthenticated);
        }
    }

    private string HashPassword(string password)
    {
        // Simple password hashing for demo purposes
        // In a real app, use a proper password hashing library
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
