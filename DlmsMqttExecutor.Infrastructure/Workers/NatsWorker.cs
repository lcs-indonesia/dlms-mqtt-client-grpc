using DlmsMqttExecutor.Application.Settings;
using DlmsMqttExecutor.Domain.Parser;
using DlmsMqttExecutor.Infrastructure.Nats;
using DlmsMqttExecutor.Infrastructure.Nats.Delegates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DlmsMqttExecutor.Infrastructure.Workers;
/// <summary>
/// NATS worker for handling DLMS requests.
/// </summary>
public partial class NatsDlmsWorker(
    NatsListener natsListener,
    NatsDlmsHandler natsDlmsHandler,
    ILogger<NatsDlmsWorker> logger,
    IOptions<AppSettings> appSettings) : BackgroundService
{
    private readonly Dictionary<string, NatsRequestDelegate<byte[], byte[]>> Handlers = [];
    private readonly int serviceIndex = appSettings.Value.NatsSubjectPattern.GetServiceIndex();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(appSettings.Value.NatsHost))
        {
            logger.LogWarning("Nats worker not started, nats host is not configured.");
            return;
        }
        logger.LogInformation("Nats Worker started");
        await natsListener.StartAsync();
        await SubscribeServices(stoppingToken);
    }
    /// <summary>
    /// Subscribes to the services for the NATS worker.
    /// </summary>
    private async Task SubscribeServices(CancellationToken cancellationToken)
    {
        var sbjs = ParseSubjectPattern(appSettings.Value.NatsSubjectPattern);
        foreach (var sbj in sbjs)
            await natsListener.SubscribeRequestAsync<byte[], byte[]>(sbj, RouteService, cancellationToken);
    }
    /// <summary>
    /// Routes the service for the NATS worker.
    /// </summary>
    private Task<byte[]> RouteService(string subject, byte[] buffer, CancellationToken cancellationToken)
    {
        var service = SubjectParser.GetSegmentAt(subject, serviceIndex);
        return natsDlmsHandler.InvokeHandler(service, buffer, cancellationToken);
    }
    /// <summary>
    /// Parses the subject pattern for the NATS worker.
    /// </summary>
    private static List<string> ParseSubjectPattern(string sbj)
    {
        var normalize1 = SubjectParser.ServiceRegex().Replace(sbj, "$1*$3");
        var sbjs = new List<string>();
        var match = SubjectParser.IndexRangeRegex().Match(normalize1);
        if (!match.Success) throw new ArgumentException(
            "Pattern invalid (prefix+).<service>.[int..int].(prefix+) or (prefix+).[int..int].<service>.(prefix+)", sbj);

        var start = int.Parse(match.Groups[2].Value);
        var end = int.Parse(match.Groups[3].Value);
        for (var i = start; i <= end; i++)
            sbjs.Add($"{match.Groups[1].Value}{i}{match.Groups[4].Value}");
        return sbjs;
    }
}
