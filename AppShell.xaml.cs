using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using VehicleServiceCenter.Pages;
using VehicleServiceCenter.Services;
using Font = Microsoft.Maui.Font;

namespace VehicleServiceCenter
{
    public partial class AppShell : Shell
    {        public AppShell()
        {
            InitializeComponent();

            // Register service route
            Routing.RegisterRoute("service", typeof(Pages.ServiceRecordDetailPage));
            Routing.RegisterRoute("settings", typeof(Pages.SettingsPage));

            // Set theme after UI is initialized
            Dispatcher.Dispatch(() =>
            {
                var currentTheme = Application.Current!.RequestedTheme;
                if (ThemeSegmentedControl != null)
                {
                    ThemeSegmentedControl.SelectedIndex = currentTheme == AppTheme.Dark ? 0 : 1;
                }
            });

            //// Configure smooth page transitions
            //Routing.AnimationEasing = Easing.CubicInOut;
            //Routing.AnimationDuration = 250;
        }
        public static async Task DisplaySnackbarAsync(string message)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF3300"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(0),
                Font = Font.SystemFontOfSize(18),
                ActionButtonFont = Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);

            await snackbar.Show(cancellationTokenSource.Token);
        }

        public static async Task DisplayToastAsync(string message)
        {
            // Toast is currently not working in MCT on Windows
            if (OperatingSystem.IsWindows())
                return;

            var toast = Toast.Make(message, textSize: 18);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await toast.Show(cts.Token);
        }

        private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Dark : AppTheme.Light;
        }
          public async Task Logout()
        {
            var authService = App.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.LogoutAsync();
            
            // Navigate to login page
            if (Application.Current?.Windows.Count > 0)
            {
                var loginPage = App.ServiceProvider.GetRequiredService<LoginPage>();
                Application.Current.Windows[0].Page = new NavigationPage(loginPage);
            }
            
            await DisplayToastAsync("You have been logged out");
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            // Ask for confirmation before logging out
            bool confirmed = await Shell.Current.DisplayAlert(
                "Confirm Logout", 
                "Are you sure you want to log out?", 
                "Yes", "No");
            
            if (confirmed)
            {
                await Logout();
            }
        }
    }
}
