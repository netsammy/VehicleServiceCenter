﻿// filepath: d:\abdus projects\MAUI\2025\VehicleServiceCenter\VehicleServiceCenter\MauiProgram.cs
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Syncfusion.Maui.Toolkit.Hosting;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.PageModels;
using VehicleServiceCenter.Pages;
using VehicleServiceCenter.Services;
using VehicleServiceCenter.Utilities;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace VehicleServiceCenter
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            // Configure the MAUI application
            builder.UseMauiApp<App>();
            
            // Configure the Community Toolkit
            builder.UseMauiCommunityToolkit(options => 
            {
                options.SetShouldSuppressExceptionsInConverters(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldEnableSnackbarOnWindows(true);
            });
            
            // Configure Syncfusion Toolkit
            builder.ConfigureSyncfusionToolkit();
              // Configure MAUI handlers
            builder.ConfigureMauiHandlers(handlers =>
            {
#if IOS || MACCATALYST
                handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
            });
            
#if WINDOWS
            // Configure Windows-specific settings
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows => windows
                    .OnWindowCreated((window) => 
                    {
                        window.ExtendsContentIntoTitleBar = false;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = AppWindow.GetFromWindowId(id);
                        appWindow.Title = AppInfo.Name;
                    }));
            });
#endif
            
            // Configure fonts
            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif
            // Register preferences service
            builder.Services.AddSingleton<IPreferences>(Preferences.Default);
            
            // Register database and auth services
            builder.Services.AddSingleton<IDatabaseService, SqlDatabaseService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            
            // Register repositories
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
            
            // Register login page
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginPageModel>();

            // Register user management page
            builder.Services.AddTransient<UserManagementPage>();
            builder.Services.AddTransient<UserManagementPageModel>();
            builder.Services.AddTransientWithShellRoute<UserManagementPage, UserManagementPageModel>("users");

            return builder.Build();
        }
    }
}
