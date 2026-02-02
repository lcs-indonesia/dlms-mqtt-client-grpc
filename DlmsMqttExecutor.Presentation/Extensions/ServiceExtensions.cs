using System.Text.Json;
using DlmsMqttExecutor.Application.Interfaces;
using DlmsMqttExecutor.Application.Services;
using DlmsMqttExecutor.Application.Settings;
using DlmsMqttExecutor.Infrastructure.Manager;
using DlmsMqttExecutor.Infrastructure.Nats;
using DlmsMqttExecutor.Infrastructure.Workers;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace DlmsMqttExecutor.Presentation.Extensions;

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

    private static IMqttClient CreateMqttClient(IServiceProvider p)
    {
        var logger = p.GetRequiredService<ILogger<IMqttClient>>();
        var mqttSettings = p.GetRequiredService<IOptions<MqttSettings>>().Value;

        logger.LogInformation("Create MqttClient connection.");
        var clientId = Guid.NewGuid().ToString();
        var builder = new MqttClientOptionsBuilder()
        .WithTcpServer(mqttSettings.Host, mqttSettings.Port)
            .WithClientId(clientId);
        if (!string.IsNullOrEmpty(mqttSettings.Username) && !string.IsNullOrEmpty(mqttSettings.Password))
            builder.WithCredentials(mqttSettings.Username, mqttSettings.Password);
        var options = builder.Build();

        var client = new MqttFactory().CreateMqttClient();
        logger.LogInformation("MqttClient created.");

        var showDisconnectLog = false;
        client.ConnectingAsync += e =>
        {
            logger.LogInformation("MqttClient connecting...");
            return Task.CompletedTask;
        };
        client.ConnectedAsync += e =>
        {
            logger.LogInformation("MqttClient connected.");
            logger.LogDebug("Config: {val}", JsonSerializer.Serialize(
            mqttSettings, options: new()
            {
                WriteIndented = true
            }));
            showDisconnectLog = true;
            return Task.CompletedTask;
        };
        client.DisconnectedAsync += async e =>
        {
            if (showDisconnectLog)
            {
                showDisconnectLog = false;
                logger.LogWarning("MqttClient disconnected, try to reconnect...");
            }
            await client.ReconnectAsync();
        };

        client.ConnectAsync(options).Wait();

        return client;
    }

    private static void Infrastructure(this IServiceCollection services)
    {
        services.AddSingleton(CreateMqttClient);
        services.AddSingleton<IDlmsClientManager, DlmsClientManager>();
        services.AddSingleton<NatsListener>();
        services.AddSingleton<NatsDlmsHandler>();
    }

    private static void Services(this IServiceCollection services)
    {
        services.AddSingleton<DlmsService>();
    }
    private static void Workers(this IServiceCollection services)
    {
        services.AddHostedService<NatsDlmsWorker>();
    }
}
