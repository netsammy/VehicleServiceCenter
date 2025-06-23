namespace VehicleServiceCenter.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(PageModels.LoginPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
