// src/HexWar.Application/Services/IGameEventPublisher.cs
namespace HexWar.Application.Services;

using HexWar.Domain.Events;

/// <summary>
/// 게임 이벤트를 외부(다른 서버, 저장소)로 발행하는 인터페이스
/// </summary>
public interface IGameEventPublisher
{
    /// <summary>
    /// 게임 이벤트를 발행합니다.
    /// - WebSocket: 직접 연결된 클라이언트에게
    /// - Redis Pub/Sub: 다른 서버에 연결된 클라이언트에게
    /// </summary>
    Task PublishAsync(string roomId, IDomainEvent domainEvent, long sequenceNumber);

    /// <summary>
    /// 특정 방의 이벤트를 구독합니다.
    /// 다른 서버에서 발행한 이벤트를 수신할 때 사용합니다.
    /// </summary>
    void Subscribe(string roomId, Func<string, Task> eventHandler);

    /// <summary>
    /// 구독을 해제합니다.
    /// </summary>
    void Unsubscribe(string roomId);
}