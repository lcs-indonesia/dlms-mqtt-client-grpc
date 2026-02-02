using System.Threading.Channels;
using DlmsMqttExecutor.Application.Settings;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
/// <summary>
/// Represents a shared MQTT client that allows simulating multiple sessions <br/>
/// while using a single underlying connection.
/// </summary>
public class MqttClientSession : IMqttClient
{
    private readonly IMqttClient client;
    private readonly ILogger logger;
    public readonly Channel<MqttApplicationMessageReceivedEventArgs> channel;

    private Task? channelTask;
    private CancellationTokenSource? cts;
    private CancellationTokenSource? linkedCts;
    public MqttClientSession(IMqttClient client, MqttSettings mqttSettings, ILogger logger)
    {
        this.client = client;
        IsConnected = false;
        Options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttSettings.Host, mqttSettings.Port)
            .WithClientId(Guid.NewGuid().ToString())
            .Build();
        this.logger = logger;
        channel = Channel.CreateUnbounded<MqttApplicationMessageReceivedEventArgs>();
    }
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

    public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;

    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

    public event Func<InspectMqttPacketEventArgs, Task>? InspectPackage;

    public bool IsConnected { get; set; }

    public MqttClientOptions Options { get; set; }
    /// <summary>
    /// Skip <see cref="MqttClient.ConnectAsync(MqttClientOptions, CancellationToken)"/>, already connect from root <br/>
    /// Create channel worker to recieve message from root (<see cref="MqttClient.ApplicationMessageReceivedAsync"/>/<see cref="client"/>)
    /// </summary>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MqttClientConnectResult?> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
    {
        IsConnected = true;

        cts = new CancellationTokenSource();
        linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        channelTask = Task.Run(async () =>
        {
            await foreach (var read in channel.Reader.ReadAllAsync(linkedCts.Token))
            {
                try
                {
                    if (ApplicationMessageReceivedAsync != null)
                        await ApplicationMessageReceivedAsync.Invoke(read);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Subcriber throw exception");
                }
            }
        }, CancellationToken.None);

        if (ConnectedAsync != null) await ConnectedAsync.Invoke(new MqttClientConnectedEventArgs(new()));
        return null;
    }
    /// <summary>
    /// Disconnecting is clean/dispose channel task <br/>
    /// Disable disconnectAsync, only from root can disconnect.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
    {
        cts?.Cancel();
        cts?.Dispose();
        try
        {
            if (channelTask != null) await channelTask;
        }
        catch (OperationCanceledException e)
        {
            logger.LogDebug("Channel Task stopped.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error on stopping channelTask: {e}", e.Message);
        }
        finally
        {
            linkedCts?.Dispose();
            cts = null;
            linkedCts = null;
            channelTask = null;
        }

        IsConnected = false;
        if (DisconnectedAsync != null) await DisconnectedAsync.Invoke(new MqttClientDisconnectedEventArgs());
    }

    public void Dispose()
    {

    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {

    }

    public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
    {
        return client.PublishAsync(applicationMessage, cancellationToken);
    }

    public async Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default)
    {

    }

    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
    {
        return client.SubscribeAsync(options, cancellationToken);
    }

    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default)
    {
        return client.UnsubscribeAsync(options, cancellationToken);
    }
}
