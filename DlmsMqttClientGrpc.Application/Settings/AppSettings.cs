namespace DlmsMqttClientGrpc.Application.Settings;

public class AppSettings
{
    public int DlmscClientChacheLimit { get; set; }
    public TimeSpan DlmsClientCacheExpiration { get; set; }
}
