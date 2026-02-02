namespace DlmsMqttExecutor.Application.Interfaces;

public interface IDlmsClientManager
{
    ISlidingItem<IDlmsClient> GetConnection(string topic, List<string> args, bool isClearCache);
}
