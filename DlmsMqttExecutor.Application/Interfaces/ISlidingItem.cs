namespace DlmsMqttExecutor.Application.Interfaces;

public interface ISlidingItem<T> : IDisposable
{
    T Value { get; }
    /// <summary>
    /// Prevent cache cleanup while using Item
    /// </summary>
    /// <returns></returns>
    IDisposable BeginRead();
    int GetTotalReader();
    void Refresh();
}
