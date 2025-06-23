using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.PageModels;

namespace VehicleServiceCenter.Services;

public class SqlDatabaseService : IDatabaseService
{
    private readonly IErrorHandler _errorHandler;
    
    public SqlDatabaseService(IErrorHandler errorHandler)
    {
        _errorHandler = errorHandler;
    }
    
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                await _errorHandler.DisplayError("Database connection string is not configured. Please update settings.");
                return;
            }
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Check if Users table exists, if not create it
            var tableExists = await CheckIfTableExistsAsync(connection, "Users");
            if (!tableExists)
            {
                await CreateUsersTableAsync(connection);
                
                // Add default admin user
                var defaultUser = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("password"),
                    IsAuthenticated = false
                };
                
                await AddDefaultUserAsync(connection, defaultUser);
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error initializing database: {ex.Message}");
        }
    }
    
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<User?> GetUserAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            string query = "SELECT Username, PasswordHash FROM Users WHERE LOWER(Username) = LOWER(@Username)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Username = reader["Username"].ToString() ?? string.Empty,
                    PasswordHash = reader["PasswordHash"].ToString() ?? string.Empty,
                    IsAuthenticated = false
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error retrieving user: {ex.Message}");
            return null;
        }
    }
    
    public async Task<List<string>> GetAllUsernamesAsync()
    {
        var usernames = new List<string>();
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            string query = "SELECT Username FROM Users ORDER BY Username";
            using var command = new SqlCommand(query, connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var username = reader["Username"].ToString();
                if (!string.IsNullOrEmpty(username))
                {
                    usernames.Add(username);
                }
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error retrieving users: {ex.Message}");
        }
        
        return usernames;
    }
    
    public async Task<bool> AddUserAsync(User user)
    {
        if (user?.Username == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Check if user already exists
            var existingUser = await GetUserAsync(user.Username);
            if (existingUser != null)
            {
                return false;
            }
            
            string query = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error adding user: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> UpdateUserAsync(User user)
    {
        if (user?.Username == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE LOWER(Username) = LOWER(@Username)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error updating user: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> DeleteUserAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return false;
        }
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            string query = "DELETE FROM Users WHERE LOWER(Username) = LOWER(@Username)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error deleting user: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> VerifyUserCredentialsAsync(string username, string passwordHash)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }
        
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            string query = "SELECT COUNT(1) FROM Users WHERE LOWER(Username) = LOWER(@Username) AND PasswordHash = @PasswordHash";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? Convert.ToInt32(result) : 0;
            return count > 0;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error verifying credentials: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> EnsureAdminUserExistsAsync()
    {
        try
        {
            var connectionString = SettingsPageModel.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                await _errorHandler.DisplayError("Database connection string is not configured. Please update settings.");
                return false;
            }
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Check if admin user exists
            var adminUser = await GetUserAsync("admin");
            if (adminUser == null)
            {
                // Create admin user with default password "password"
                var defaultUser = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("password"),
                    IsAuthenticated = false
                };
                
                await AddDefaultUserAsync(connection, defaultUser);
                await _errorHandler.DisplaySuccess("Admin user created successfully. Username: admin, Password: password");
                return true;
            }
            
            await _errorHandler.DisplaySuccess("Admin user already exists");
            return true;
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error creating admin user: {ex.Message}");
            return false;
        }
    }
    
    private async Task<bool> CheckIfTableExistsAsync(SqlConnection connection, string tableName)
    {
        string query = @"
            SELECT COUNT(1) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = @TableName";
            
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        
        var result = await command.ExecuteScalarAsync();
        var count = result != null ? Convert.ToInt32(result) : 0;
        return count > 0;
    }
    
    private async Task CreateUsersTableAsync(SqlConnection connection)
    {
        string query = @"
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(50) NOT NULL UNIQUE,
                PasswordHash NVARCHAR(100) NOT NULL,
                CreatedDate DATETIME DEFAULT GETDATE()
            )";
            
        using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();
    }
    
    private async Task AddDefaultUserAsync(SqlConnection connection, User user)
    {
        if (user?.Username == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return;
        }
        
        string query = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash)";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        
        await command.ExecuteNonQueryAsync();
    }
    
    private string HashPassword(string password)
    {
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
