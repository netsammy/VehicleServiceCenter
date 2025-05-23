using Microsoft.Maui.Controls;

namespace VehicleServiceCenter.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransientWithShellRoute<TPage>(
        this IServiceCollection services,
        string route) where TPage : Page
    {
        services.AddTransient<TPage>();
        Routing.RegisterRoute(route, typeof(TPage));
        return services;
    }

    public static IServiceCollection AddTransientWithShellRoute<TPage, TViewModel>(
        this IServiceCollection services,
        string route) 
        where TPage : Page
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TPage>();
        Routing.RegisterRoute(route, typeof(TPage));
        return services;
    }
}