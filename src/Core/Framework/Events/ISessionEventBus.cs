using MengYou.Abstractions.Events;

namespace MengYou.Abstractions;

/// <summary>
/// 会话级事件总线：每个 GameSession 一个实例，避免多开互相干扰。
/// </summary>
public interface ISessionEventBus
{
    /// <summary>发布事件。</summary>
    void Publish<TEvent>(TEvent evt) where TEvent : GameEvent;

    /// <summary>订阅事件；返回 IDisposable 用于取消订阅。</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent;
}
