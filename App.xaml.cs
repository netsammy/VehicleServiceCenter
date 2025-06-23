using Microsoft.Extensions.DependencyInjection;
using VehicleServiceCenter.Services;
using VehicleServiceCenter.Pages;

namespace VehicleServiceCenter
{    public partial class App : Application
    {
        private const string ThemePreferenceKey = "AppTheme";
        private readonly IPreferences _preferences;
        private IAuthService? _authService;

        public App()
        {
            InitializeComponent();
            _preferences = Preferences.Default;
            
            // Don't try to get auth service here - it's not ready yet
            // We'll initialize it in CreateWindow instead
            
            LoadTheme();
        }

        private void LoadTheme()
        {
            // Get saved theme or default to Light
            var savedTheme = _preferences.Get(ThemePreferenceKey, "Light");
            UserAppTheme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
        }

        public void SetTheme(AppTheme theme)
        {
            if (theme != UserAppTheme)
            {
                UserAppTheme = theme;
                _preferences.Set(ThemePreferenceKey, theme.ToString());
            }
        }        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                // Initialize the auth service
                _authService = ServiceProvider.GetRequiredService<IAuthService>();

                // Check if user is authenticated
                if (_authService.IsAuthenticated)
                {
                    // User is authenticated, show the main app shell
                    return new Window(new AppShell());
                }
                else
                {
                    // User is not authenticated, show the login page
                    var loginPage = ServiceProvider.GetRequiredService<LoginPage>();
                    return new Window(new NavigationPage(loginPage));
                }
            }
            catch (Exception ex)
            {
                // If there's an error, just show the AppShell
                System.Diagnostics.Debug.WriteLine($"Error initializing window: {ex}");
                return new Window(new AppShell());
            }
        }        // Easy access to the service provider
        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (Current?.Handler?.MauiContext == null)
                    throw new InvalidOperationException("ServiceProvider is not available yet.");
                    
                return Current.Handler.MauiContext.Services;
            }
        }
    }
}