using Microsoft.Data.SqlClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Security;

namespace VehicleServiceCenter.PageModels;

public partial class SettingsPageModel : ObservableObject
{
    private readonly IPreferences _preferences;
    private const string ThemePreferenceKey = "AppTheme";

    [ObservableProperty]
    private string _currentTheme;

    [ObservableProperty]
    private string _serverName;

    [ObservableProperty]
    private string _databaseName;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;    [ObservableProperty]
    private string _receiptPrefix = string.Empty;

    [ObservableProperty]
    private int _defaultRecordLimit;

    public SettingsPageModel(IPreferences preferences)
    {
        _preferences = preferences;
        // Initialize fields to empty string to satisfy non-null requirements
        _serverName = string.Empty;
        _databaseName = string.Empty;
        _username = string.Empty;
        _password = string.Empty;
        _currentTheme = _preferences.Get(ThemePreferenceKey, "Light");
        LoadSettings();
    }

    [RelayCommand]
    private Task Appearing()
    {
        LoadSettings();
        return Task.CompletedTask;
    }

    private void LoadSettings()
    {        ServerName = _preferences.Get("DbServerName", "");
        DatabaseName = _preferences.Get("DbDatabaseName", "");
        Username = _preferences.Get("DbUsername", "");
        Password = _preferences.Get("DbPassword", "");
        ReceiptPrefix = _preferences.Get("ReceiptPrefix", "VSC");
        DefaultRecordLimit = _preferences.Get("DefaultRecordLimit", 20);
        CurrentTheme = _preferences.Get(ThemePreferenceKey, "Light");
    }

    public void UpdateTheme(string theme)
    {
        CurrentTheme = theme;
        _preferences.Set(ThemePreferenceKey, theme);

        if (Application.Current is App app)
        {
            app.SetTheme(theme == "Dark" ? AppTheme.Dark : AppTheme.Light);
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {        _preferences.Set("DbServerName", ServerName);
        _preferences.Set("DbDatabaseName", DatabaseName);
        _preferences.Set("DbUsername", Username);
        _preferences.Set("DbPassword", Password);
        _preferences.Set("ReceiptPrefix", ReceiptPrefix);
        _preferences.Set("DefaultRecordLimit", DefaultRecordLimit);
        _preferences.Set(ThemePreferenceKey, CurrentTheme);

        await Shell.Current.DisplayAlert("Success", "Settings saved successfully", "OK");
    }    [RelayCommand]
    private async Task TestConnection()
    {
        try
        {
            var connStr = GetConnectionString();
            
            using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();
            
            // Check if Users table exists, if not create it
            bool tableExists = await CheckIfTableExistsAsync(connection, "Users");
            if (!tableExists)
            {
                bool created = await CreateUsersTableAsync(connection);
                if (created)
                {
                    await Shell.Current.DisplayAlert("Success", "Connection test successful and Users table created", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Success", "Connection test successful but failed to create Users table", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Success", "Connection test successful", "OK");
            }
        }
        catch (SqlException ex)
        {
            var details = $"Error Number: {ex.Number}\nMessage: {ex.Message}\nServer: {ex.Server}";
            await Shell.Current.DisplayAlert("SQL Error", details, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Connection test failed: {ex.GetType().Name} - {ex.Message}", "OK");
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
        int count = result != null ? Convert.ToInt32(result) : 0;
        return count > 0;
    }
    
    private async Task<bool> CreateUsersTableAsync(SqlConnection connection)
    {
        try
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
            
            // Add default admin user
            string adminQuery = @"
                INSERT INTO Users (Username, PasswordHash)
                VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918')";
                
            using var adminCommand = new SqlCommand(adminQuery, connection);
            await adminCommand.ExecuteNonQueryAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private async Task CreateAdminUser()
    {
        try
        {
            // First, test the connection
            var connStr = GetConnectionString();
            
            using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();
            
            // Check if Users table exists, if not create it
            bool tableExists = await CheckIfTableExistsAsync(connection, "Users");
            if (!tableExists)
            {
                bool created = await CreateUsersTableAsync(connection);
                if (!created)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to create Users table", "OK");
                    return;
                }
            }
            
            // Check if admin user exists
            bool adminExists = await CheckIfUserExistsAsync(connection, "admin");
            if (adminExists)
            {
                await Shell.Current.DisplayAlert("Info", "Admin user already exists", "OK");
                return;
            }
            
            // Create admin user
            string adminQuery = @"
                INSERT INTO Users (Username, PasswordHash)
                VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918')";
                
            using var adminCommand = new SqlCommand(adminQuery, connection);
            await adminCommand.ExecuteNonQueryAsync();
            
            await Shell.Current.DisplayAlert("Success", "Admin user created successfully. Username: admin, Password: password", "OK");
        }
        catch (SqlException ex)
        {
            var details = $"Error Number: {ex.Number}\nMessage: {ex.Message}\nServer: {ex.Server}";
            await Shell.Current.DisplayAlert("SQL Error", details, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to create admin user: {ex.GetType().Name} - {ex.Message}", "OK");
        }
    }
    
    private async Task<bool> CheckIfUserExistsAsync(SqlConnection connection, string username)
    {
        string query = @"
            SELECT COUNT(1) 
            FROM Users 
            WHERE LOWER(Username) = LOWER(@Username)";
            
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Username", username);
        
        var result = await command.ExecuteScalarAsync();
        int count = result != null ? Convert.ToInt32(result) : 0;
        return count > 0;
    }

    public static string GetConnectionString()
    {
        var preferences = Preferences.Default;
        if (preferences == null)
            return string.Empty;

        var server = preferences.Get("DbServerName", "");
        var database = preferences.Get("DbDatabaseName", "");
        var username = preferences.Get("DbUsername", "");
        var password = preferences.Get("DbPassword", "");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database           
        };

        if (string.IsNullOrWhiteSpace(username))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password;
        }

        return builder.ConnectionString+ ";TrustServerCertificate=true";
    }
}