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
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        try
        {
            var connStr = GetConnectionString();
            await Shell.Current.DisplayAlert("Connection String", connStr, "OK");
            
            using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();
            await Shell.Current.DisplayAlert("Success", "Connection test successful", "OK");
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