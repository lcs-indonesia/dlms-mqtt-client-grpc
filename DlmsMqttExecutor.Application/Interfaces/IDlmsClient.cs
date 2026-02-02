
using DlmsMqttExecutor.Application.DTOs.Dlms;

namespace DlmsMqttExecutor.Application.Interfaces;

public interface IDlmsClient : IDisposable
{
    void SetDisconnectControl(bool value, DlmsReadObjectFilterDto filter, string? ln = null, CancellationToken cancellationToken = default);
    void ExecuteScript(string ln, int scriptId, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default);
    IEnumerable<object> ReadObject(List<KeyValuePair<string, int>> readObjects, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default);
    object ReadObject(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default);
    ProfileGenericValuesDto ReadProfileGenericValue(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default);
}
