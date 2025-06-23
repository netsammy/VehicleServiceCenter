using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class UserManagementPageModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IErrorHandler _errorHandler;

    [ObservableProperty]
    private string _newUsername = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _selectedUsername = string.Empty;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newUserPassword = string.Empty;

    [ObservableProperty]
    private string _confirmUserPassword = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAddUserVisible;

    [ObservableProperty]
    private bool _isChangePasswordVisible;

    [ObservableProperty]
    private ObservableCollection<string> _usernames = new();

    public UserManagementPageModel(IAuthService authService, IErrorHandler errorHandler)
    {
        _authService = authService;
        _errorHandler = errorHandler;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            IsLoading = true;
            var users = await _authService.GetAllUsernamesAsync();
            Usernames.Clear();

            foreach (var username in users)
            {
                Usernames.Add(username);
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error loading users: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowAddUser()
    {
        NewUsername = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        IsAddUserVisible = true;
        IsChangePasswordVisible = false;
    }

    [RelayCommand]
    private void ShowChangePassword()
    {
        CurrentPassword = string.Empty;
        NewUserPassword = string.Empty;
        ConfirmUserPassword = string.Empty;
        IsAddUserVisible = false;
        IsChangePasswordVisible = true;
    }

    [RelayCommand]
    private async Task AddUser()
    {
        if (string.IsNullOrWhiteSpace(NewUsername))
        {
            await _errorHandler.DisplayError("Username is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            await _errorHandler.DisplayError("Password is required");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            await _errorHandler.DisplayError("Passwords do not match");
            return;
        }

        try
        {
            IsLoading = true;
            var success = await _authService.AddUserAsync(NewUsername, NewPassword);            if (success)
            {
                await _errorHandler.DisplayMessage($"User '{NewUsername}' added successfully", "Success");
                IsAddUserVisible = false;
                await LoadDataCommand.ExecuteAsync(null);
            }
            else
            {
                await _errorHandler.DisplayError($"Failed to add user '{NewUsername}'. Username may already exist.");
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error adding user: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(SelectedUsername))
        {
            await _errorHandler.DisplayError("Please select a user");
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            await _errorHandler.DisplayError("Current password is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewUserPassword))
        {
            await _errorHandler.DisplayError("New password is required");
            return;
        }

        if (NewUserPassword != ConfirmUserPassword)
        {
            await _errorHandler.DisplayError("New passwords do not match");
            return;
        }

        try
        {
            IsLoading = true;
            var success = await _authService.ChangePasswordAsync(
                SelectedUsername, 
                CurrentPassword, 
                NewUserPassword);            if (success)
            {
                await _errorHandler.DisplayMessage("Password changed successfully", "Success");
                IsChangePasswordVisible = false;
            }
            else
            {
                await _errorHandler.DisplayError("Failed to change password. Please check your current password.");
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error changing password: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsAddUserVisible = false;
        IsChangePasswordVisible = false;
    }
    
    [RelayCommand]
    private async Task CreateAdminUser()
    {
        try
        {
            IsLoading = true;
            
            // Check if admin user already exists
            var users = await _authService.GetAllUsernamesAsync();
            bool adminExists = users.Any(u => u.Equals("admin", StringComparison.OrdinalIgnoreCase));
            
            if (adminExists)
            {
                await _errorHandler.DisplaySuccess("Admin user already exists");
                return;
            }
            
            // Create admin user with default password
            var success = await _authService.AddUserAsync("admin", "password");
            
            if (success)
            {
                await _errorHandler.DisplaySuccess("Admin user created successfully. Username: admin, Password: password");
                await LoadDataCommand.ExecuteAsync(null);
            }
            else
            {
                await _errorHandler.DisplayError("Failed to create admin user");
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.DisplayError($"Error creating admin user: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
