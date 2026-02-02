using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Channels;
using DLMS.Client;
using DLMS.Client.GXMedia.Mqtt;
using DlmsMqttExecutor.Application.Interfaces;
using DlmsMqttExecutor.Application.Settings;
using DlmsMqttExecutor.Infrastructure.Caching;
using Gurux.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;

namespace DlmsMqttExecutor.Infrastructure.Manager;

public class DlmsClientManager : IDlmsClientManager
{
    private readonly IOptions<AppSettings> appSettings;
    private readonly ConcurrentDictionary<string, SlidingItem<IDlmsClient>> dlmsClientCache = new();
    private readonly ILogger logger;
    private readonly IOptions<MqttSettings> mqttSettings;
    private readonly ConcurrentDictionary<string, Channel<MqttApplicationMessageReceivedEventArgs>> sessionChannels = [];
    private bool isConnected = false;
    private IMqttClient mqttClient;
    public DlmsClientManager(
        IMqttClient mqttClient,
        IOptions<AppSettings> appSettings,
        IOptions<MqttSettings> mqttSettings,
        ILogger<DlmsClientManager> logger)
    {
        this.appSettings = appSettings;
        this.mqttSettings = mqttSettings;
        this.logger = logger;

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var channel = sessionChannels.GetValueOrDefault(e.ApplicationMessage.Topic);
            if (channel != null) await channel.Writer.WriteAsync(e);
        };
        this.mqttClient = mqttClient;
    }
    /// <summary>
    /// Only allow one proccess per topic. <br/>
    /// Use <see cref="ISlidingItem{T}.BeginRead"/> to prevent cache cleanup while using Item
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="args"></param>
    /// <returns/>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="Exception"></exception>
    public ISlidingItem<IDlmsClient> GetConnection(string topic, List<string> args, bool isClearCache)
    {
        logger.LogDebug("Get or Create dlms client connection.");

        var filePath = AddCustomCacheFolder(args, appSettings.Value.DlmsCacheFolderPath);

        var cache = dlmsClientCache.GetValueOrDefault(topic);
        if (cache != null && cache.GetTotalReader() > 0)
            throw new OperationCanceledException("There is reader on topic: " + topic);

        if (isClearCache)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            dlmsClientCache.TryRemove(topic, out cache);
            cache?.Dispose();
            cache = null;
        }
        if (cache == null)
        {
            logger.LogDebug("Creating dlms client connection...");

            var gxMqtt = CreateGxMqtt(topic);
            var settings = new Settings { media = gxMqtt };
            var client = new DLMSClient([.. args], settings);
            logger.LogDebug("Dlms client connected");

            logger.LogDebug("Adding new dlms client to session...");

            cache = new SlidingItem<IDlmsClient>(topic, client,
                appSettings.Value.DlmsClientCacheExpiration, OnCacheExpiration);
            dlmsClientCache.AddOrUpdate(topic, cache, (_, old) =>
            {
                old.Dispose();
                return cache;
            });
            logger.LogDebug("Session added with expiration: {seconds}s",
                appSettings.Value.DlmsClientCacheExpiration.TotalSeconds);
        }

        if (cache == null) throw new Exception("Dlms Client is null.");
        cache.Refresh();
        return cache;
    }
    private string? AddCustomCacheFolder(List<string> args, string cacheFolderPath)
    {
        var index = args.IndexOf("-o");
        if (index == -1) return null;
        if (index + 1 >= args.Count)
            throw new InvalidEnumArgumentException("No value after argument '-o'.");

        var folder = Path.GetFullPath(cacheFolderPath);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var result = Path.Combine(folder, args[index + 1]);
        args[index + 1] = result;
        return result;
    }

    /// <summary>
    /// Create mqtt client session per topic
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    private IGXMedia CreateGxMqtt(string topic)
    {
        logger.LogDebug("Creating new mqtt session...");
        var session = new MqttClientSession(mqttClient, mqttSettings.Value, logger);
        sessionChannels.AddOrUpdate(topic, (_) => session.channel, (_, _) => session.channel);
        sessionChannels.AddOrUpdate(session.Options.ClientId, (_) => session.channel, (_, _) => session.channel);
        logger.LogDebug("Mqtt session created.");

        logger.LogDebug("Creating gxMqtt client");
        var client = new GXMqtt(session);
        logger.LogDebug("GxMqtt created.");
        return client;
    }

    private void OnCacheExpiration(string key)
    {
        logger.LogDebug("Cache expired, cleaning session...");
        if (dlmsClientCache.TryRemove(key, out var client)) client.Value.Dispose();
        sessionChannels.TryRemove(key, out _);
    }
}
