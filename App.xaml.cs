using Microsoft.Extensions.DependencyInjection;

namespace VehicleServiceCenter
{
    public partial class App : Application
    {
        private const string ThemePreferenceKey = "AppTheme";
        private readonly IPreferences _preferences;

        public App()
        {
            InitializeComponent();
            _preferences = Preferences.Default;
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
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}