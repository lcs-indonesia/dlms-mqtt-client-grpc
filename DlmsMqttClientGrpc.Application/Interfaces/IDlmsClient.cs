
namespace DlmsMqttClientGrpc.Application.Interfaces;

public interface IDlmsClient
{
    IEnumerable<object> ReadObject(List<KeyValuePair<string, int>> readObjects);
}
