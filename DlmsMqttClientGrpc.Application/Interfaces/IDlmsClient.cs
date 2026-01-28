
using DlmsMqttClientGrpc.Application.DTOs.Dlms;

namespace DlmsMqttClientGrpc.Application.Interfaces;

public interface IDlmsClient : IDisposable
{
    void SetDisconnectControl(bool value,DlmsReadObjectFilterDto filter,string? ln = null);
    void ExecuteScript(string ln, int scriptId,DlmsReadObjectFilterDto filter);
    IEnumerable<object> ReadObject(List<KeyValuePair<string, int>> readObjects, DlmsReadObjectFilterDto filter);
    object ReadObject(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter);
    ProfileGenericValuesDto ReadProfileGenericValue(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter);
}
