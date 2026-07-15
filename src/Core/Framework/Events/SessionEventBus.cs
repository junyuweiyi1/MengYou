namespace iFramework;

/// <summary>
/// 会话级事件总线实现：内存分发，按事件类型订阅。
/// </summary>
public sealed class SessionEventBus : ISessionEventBus
{
    /// <summary>订阅表：Key=事件类型，Value=处理器列表。</summary>
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>同步锁，保护订阅表的增删遍历。</summary>
    private readonly object _sync = new();

    /// <inheritdoc/>
    public void Publish<TEvent>(TEvent evt) where TEvent : GameEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list)) return;
        Delegate[] snapshot;
        lock (_sync) snapshot = list.ToArray();
        foreach (var d in snapshot)
        {
            try { ((Action<TEvent>)d)(evt); }
            catch { /* 单个订阅者异常不影响其他 */ }
        }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (_sync) list.Add(handler);
        return new Subscription(() =>
        {
            lock (_sync) list.Remove(handler);
        });
    }

    /// <summary>订阅句柄：Dispose 时移除处理器。</summary>
    private sealed class Subscription : IDisposable
    {
        /// <summary>取消订阅动作。</summary>
        private readonly Action _dispose;

        /// <summary>是否已释放。</summary>
        private bool _disposed;

        /// <summary>构造。</summary>
        public Subscription(Action dispose) => _dispose = dispose;

        /// <summary>取消订阅。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dispose();
        }
    }
}
