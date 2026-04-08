using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using AzureDesktop.Services;
using AzureDesktop.ViewModels;

namespace AzureDesktop;

public partial class App : Application
{
    private Window? _window;
    private static IServiceProvider? _serviceProvider;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();

        // Services (singletons so auth state is shared)
        services.AddSingleton<IAzureAuthService, AzureAuthService>();
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<IApplicationGatewayService, ApplicationGatewayService>();
        services.AddSingleton<ITagService, TagService>();
        services.AddSingleton<ILockService, LockService>();

        // ViewModels
        services.AddSingleton<SubscriptionsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SubscriptionDetailViewModel>();
        services.AddTransient<ResourceGroupsViewModel>();
        services.AddTransient<ResourcesViewModel>();
        services.AddTransient<TagManagerViewModel>();
        services.AddTransient<LockManagerViewModel>();
        services.AddTransient<ApplicationGatewayViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public static T GetService<T>() where T : class =>
        _serviceProvider!.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
