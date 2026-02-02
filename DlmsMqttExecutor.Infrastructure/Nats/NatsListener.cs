using DlmsMqttExecutor.Application.Settings;
using DlmsMqttExecutor.Infrastructure.Nats.Delegates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace DlmsMqttExecutor.Infrastructure.Nats;

public class NatsListener(
    IOptions<AppSettings> appSettings,
    ILogger<NatsListener> logger,
    IHostApplicationLifetime lifetime)
{
    private readonly ILogger<NatsListener> logger = logger;
    private readonly NatsConnection con = new(new() { Url = appSettings.Value.NatsHost });

    public async Task StartAsync()
    {
        logger.LogInformation("Starting NatsListener");
        await con.ConnectAsync();
    }
    public Task SubscribeRequestAsync<TRequest, TReply>(string subject, NatsRequestDelegate<TRequest, TReply> handler,
        CancellationToken cancellationToken = default)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, cancellationToken);
        var msgs = con.SubscribeAsync<TRequest>(subject, cancellationToken: linkedCts.Token);
        _ = RunRequestRequestLoopAsync(msgs, handler, cancellationToken);
        return Task.CompletedTask;
    }
    private static async Task RunRequestRequestLoopAsync<TRequest, TReply>(IAsyncEnumerable<NatsMsg<TRequest>> messages,
        NatsRequestDelegate<TRequest, TReply> handler, CancellationToken cancellationToken)
    {
        await foreach (var message in messages)
            _ = ProcessRequest(message, handler, cancellationToken);
    }

    private static async Task ProcessRequest<TRequest, TReply>(
        NatsMsg<TRequest> message, NatsRequestDelegate<TRequest, TReply> handler, CancellationToken cancellationToken)
    {
        if (message.Data == null) return;
        var reply = await handler(message.Subject, message.Data, cancellationToken);
        await message.ReplyAsync(reply, cancellationToken: cancellationToken);
    }
}
