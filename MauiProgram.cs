using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.PageModels;
using VehicleServiceCenter.Pages;
using VehicleServiceCenter.Services;
using VehicleServiceCenter.Utilities;

namespace VehicleServiceCenter
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif            // Register preferences service
            builder.Services.AddSingleton<IPreferences>(Preferences.Default);            // Register repositories
            builder.Services.AddSingleton<IServiceRepository, ServiceRepository>();
            
            // Register error handler
            builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();

            // Register pages and view models
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddTransientWithShellRoute<ServiceRecordListPage, ServiceRecordListPageModel>("services");
            builder.Services.AddTransientWithShellRoute<ServiceRecordDetailPage, ServiceRecordDetailPageModel>("service");
            
            // Register SettingsPageModel as singleton since we want to persist theme state
            builder.Services.AddSingleton<SettingsPageModel>();
            builder.Services.AddTransientWithShellRoute<SettingsPage>("settings");

            return builder.Build();
        }
    }
}
