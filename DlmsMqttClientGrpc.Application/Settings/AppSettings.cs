namespace DlmsMqttClientGrpc.Application.Settings;

public class AppSettings
{
    public string DlmsCacheFolderPath { get; set; } = null!;
    public int DlmscClientChacheLimit { get; set; }
    public TimeSpan DlmsClientCacheExpiration { get; set; }
    public string NatsHost { get; set; } = "";
    public string NatsSubjectPattern { get; set; } = "";
}
