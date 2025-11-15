using DlmsMqttClientGrpc.Application.Interfaces;
using DlmsMqttClientGrpc.Infrastructure.Manager;

namespace DlmsMqttClientGrpc.Presentation.Extensions;

public static class ServiceExtensions
{
    public static void AddServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddServices();
    }
    public static void AddServices(this IServiceCollection services)
    {
        services.Services();
        services.Infrastructure();
        services.Workers();
        services.Commons();
    }
    private static void Commons(this IServiceCollection services)
    {
    }

    private static void Infrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDlmsClientManager, DlmsClientManager>();
    }

    private static void Services(this IServiceCollection services)
    {
        //services.AddSingleton<DlmsService>();
    }
    private static void Workers(this IServiceCollection services)
    {

    }
}
