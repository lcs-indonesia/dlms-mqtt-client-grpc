using DlmsMqttExecutor.Application.Settings;
using DotNetEnv;

namespace DlmsMqttExecutor.Presentation.Extensions;

public static class SettingsExtension
{
    public const string Prefix = "DLMS_MQTT_CLIENT_GRPC_";
    public static void AddSettings(this IHostApplicationBuilder builder)
    {
        Env.Load();
        builder.Configuration.AddEnvironmentVariables(prefix: Prefix);
        builder.Services.AddSettings(builder.Configuration);
    }
    public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var missingFields = new List<string>();
        services.Configure<AppSettings>(configuration.TryValidateAndGetSection<AppSettings>(ref missingFields));
        services.Configure<MqttSettings>(configuration.TryValidateAndGetSection<MqttSettings>(ref missingFields));

        if (missingFields.Any()) throw new Exception("\n" + string.Join('\n', missingFields));
    }
    private static IConfigurationSection TryValidateAndGetSection<T>(this IConfiguration config,
        ref List<string> missingFields) where T : class, new()
    {
        var t = new T();
        var type = t.GetType();
        var section = config.GetSection(type.Name);
        section.Bind(t);

        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(t);
            if (value == null) missingFields.Add($"- {type.Name}.{prop.Name} is null.");
        }
        return section;
    }
}
