using DotNetEnv;
using Serilog;
using Serilog.Events;

namespace DlmsMqttClientGrpc.Presentation.Extensions;

public static class LogExtensions
{
    public static void AddLogger(this IHostApplicationBuilder builder)
    {
        Env.Load();
        var logMinLevel = Env.GetString("LOG_MIN_LEVEL", nameof(LogEventLevel.Information));
        var level = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), logMinLevel);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u4}] {Message:lj} ({SourceContext}){NewLine}{Exception}")
            .Enrich.FromLogContext()
            .CreateLogger();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }
}
