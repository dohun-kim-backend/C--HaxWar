namespace HexWar.Domain.Events;

using HexWar.Domain.Entities;
using HexWar.Domain.Enums;
using HexWar.Domain.ValueObjects;

public interface IDomainEvent
{
    /// <summary>이벤트 발생 시간</summary>
    DateTime OccurredAt { get; }

    /// <summary>이벤트가 발생한 게임방 ID</summary>
    string RoomId { get; }

    /// <summary>이벤트 유형 (직렬화/로깅용)</summary>
    string EventType { get; }
}

/// <summary>
/// 도메인 이벤트 추상 기본 클래스
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string RoomId { get; init; }
    public string EventType => GetType().Name;

    protected DomainEvent(string roomId)
    {
        RoomId = roomId;
    }
}