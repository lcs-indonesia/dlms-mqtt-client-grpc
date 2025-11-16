using DlmsMqttClientGrpc.Application.Interfaces;

namespace DlmsMqttClientGrpc.Infrastructure.Caching;

public class SlidingItem<T> : ISlidingItem<T>
{
    private readonly Action<string>? callback;
    private readonly string key;
    private readonly TimeSpan timeout;
    private readonly Timer timer;
    private int dispose = 0;
    private int readers = 0;
    public SlidingItem(string key, T value, TimeSpan timeout, Action<string>? callback)
    {
        this.callback = callback;
        this.key = key;
        Value = value;
        this.timeout = timeout;

        timer = new(OnTimer, null, timeout, Timeout.InfiniteTimeSpan);
    }

    public T Value { get; }
    private bool IsDisposed => dispose == 1;
    public IDisposable BeginRead()
    {
        if (IsDisposed) throw new ObjectDisposedException(key);

        Interlocked.Increment(ref readers);

        return new EndReadDisposable(() =>
        {
            Interlocked.Decrement(ref readers);
            Refresh();
        });

    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref dispose, 1) == 1) return;

        timer.Dispose();
        callback?.Invoke(key);
    }

    public int GetTotalReader() => readers;
    public void Refresh()
    {
        if (IsDisposed) return;

        timer.Change(timeout, Timeout.InfiniteTimeSpan);
    }
    private void OnTimer(object? state)
    {
        if (IsDisposed) return;

        if (Interlocked.CompareExchange(ref readers, 0, 0) > 0)
        {
            timer.Change(TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan);
            return;
        }

        Dispose();
    }
    private class EndReadDisposable(Action end) : IDisposable
    {
        private readonly Action end = end;
        public void Dispose() => end();
    }
}