
using DlmsMqttClientGrpc.Application.DTOs.Dlms;

namespace DlmsMqttClientGrpc.Application.Interfaces;

public interface IDlmsClient : IDisposable
{
    IEnumerable<object> ReadObject(List<KeyValuePair<string, int>> readObjects, DlmsReadObjectFilterDto filter);
}
