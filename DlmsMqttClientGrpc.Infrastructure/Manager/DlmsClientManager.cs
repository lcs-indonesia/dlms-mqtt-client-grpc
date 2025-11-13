using System.Text.Json;
using DLMS.Client;
using DLMS.Client.GXMedia.Mqtt;
using DlmsMqttClientGrpc.Application.Interfaces;
using DlmsMqttClientGrpc.Application.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DlmsMqttClientGrpc.Infrastructure.Manager;

public class DlmsClientManager : IDlmsClientManager
{
    private readonly MemoryCache dlmsClientCache;
    private readonly GXMqtt gxMqtt;
    private readonly IOptions<AppSettings> appSettings;
    private readonly ILogger logger;

    public DlmsClientManager(
        IOptions<AppSettings> appSettings,
        IOptions<MqttSettings> mqttSettings,
        ILogger<DlmsClientManager> logger)
    {
        dlmsClientCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = appSettings.Value.DlmscClientChacheLimit,
        });
        logger.LogInformation("Creating base GxMqtt");
        gxMqtt = new()
        {
            Port = mqttSettings.Value.Port,
            ServerAddress = mqttSettings.Value.Host,
            ClientId = Guid.NewGuid().ToString(),
            Username = mqttSettings.Value.Username,
            Password = mqttSettings.Value.Password,
        };
        logger.LogInformation("GxMqtt created with config: {val}", JsonSerializer.Serialize(
            mqttSettings.Value, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        this.appSettings = appSettings;
        this.logger = logger;
    }
    public IDlmsClient GetConnection(string sessionId, string[] args)
    {
        logger.LogDebug("Get or Create dlms client connection.");
        if (!dlmsClientCache.TryGetValue(sessionId, out DLMSClient? client))
        {
            logger.LogDebug("Creating dlms client connection...");
            var settings = new Settings { media = gxMqtt };
            client = new DLMSClient(args, settings);
            logger.LogDebug("Dlms client connected");

            logger.LogDebug("Adding new dlms client to session...");
            dlmsClientCache.Set(sessionId, client, new MemoryCacheEntryOptions
            {
                SlidingExpiration = appSettings.Value.DlmsClientCacheExpiration,
                Size = 1,

            });
            logger.LogDebug("Session added with expiration: {seconds}s",
                appSettings.Value.DlmsClientCacheExpiration.TotalSeconds);
        }

        if (client == null) throw new Exception("Dlms Client is null.");
        return client;
    }
}
