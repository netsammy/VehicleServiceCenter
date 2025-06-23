using VehicleServiceCenter.PageModels;

namespace VehicleServiceCenter.Pages;

public partial class UserManagementPage : ContentPage
{
    public UserManagementPage(UserManagementPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
