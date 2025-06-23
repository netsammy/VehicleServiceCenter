using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace VehicleServiceCenter.PageModels;

public partial class LoginPageModel : ObservableObject
{
    private readonly IAuthService _authService;    private readonly IErrorHandler _errorHandler;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public LoginPageModel(IAuthService authService, IErrorHandler errorHandler)
    {
        _authService = authService;
        _errorHandler = errorHandler;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            await _errorHandler.DisplayError("Username is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            await _errorHandler.DisplayError("Password is required");
            return;
        }        try
        {
            IsLoading = true;
            
            var success = await _authService.LoginAsync(Username, Password);
            
            if (success)
            {
                // Navigate to main page on successful login
                // Instead of using Shell navigation, we'll set the main page directly
                // This is safer when the login page isn't part of a Shell yet
                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                });
            }
            else
            {
                await _errorHandler.DisplayError("Invalid username or password");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login error: {ex.Message}");
            await _errorHandler.DisplayError("An error occurred during login");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }
}
