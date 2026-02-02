namespace DlmsMqttExecutor.Application.Settings;

public class MqttSettings
{
    public int Port { get; set; }
    public string Host { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
