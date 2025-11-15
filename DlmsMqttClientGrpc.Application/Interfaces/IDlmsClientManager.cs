namespace DlmsMqttClientGrpc.Application.Interfaces;

public interface IDlmsClientManager
{
    IDlmsClient GetConnection(string sessionId, List<string> args);
}
