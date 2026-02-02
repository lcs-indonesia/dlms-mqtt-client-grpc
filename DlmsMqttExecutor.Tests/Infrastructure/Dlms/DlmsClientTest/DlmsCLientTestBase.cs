using DlmsMqttExecutor.Application.Interfaces;
using DlmsMqttExecutor.Tests._Helpers;

namespace Infrastructure.Dlms.DlmsClientTest;

[Collection(nameof(ApplicationCollection))]
public class DlmsCLientTestBase : TestBase
{
    internal ISlidingItem<IDlmsClient> sliding;

    public DlmsCLientTestBase()
    {
        var manager = fixture.GetService<IDlmsClientManager>();
        var topic = "9a7a2bcf-b93c-44cd-8083-b0fa659ab143-1";
        var args = new List<string> { "-c", "18", "-a", "High", "-P", "Gurux", "-w", "1", "-f", "128", "-t", "Verbose", "-o", "test-local.xml", "-q", topic };
        sliding = manager.GetConnection(topic, args, false);
    }
}
