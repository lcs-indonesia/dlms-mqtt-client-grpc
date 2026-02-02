namespace DlmsMqttExecutor.Tests._Helpers;

public class TestBase : IAsyncLifetime
{
    internal TestFixture fixture = new();
    internal readonly DateTime now = DateTime.UtcNow;
    internal const string createBy = "Test";
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        fixture.Dispose();
        return Task.CompletedTask;
    }
}
