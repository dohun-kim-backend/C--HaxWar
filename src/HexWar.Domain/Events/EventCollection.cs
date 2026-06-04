// src/HexWar.Domain/Events/EventCollection.cs
namespace HexWar.Domain.Events;

/// <summary>
/// 한 라운드에서 발생한 모든 이벤트의 컬렉션
/// 클라이언트 전송용 집계 객체
/// </summary>
public sealed class RoundEventCollection
{
    public string RoomId { get; init; }
    public int RoundNumber { get; init; }

    /// <summary>공개 이벤트 (모든 플레이어에게 전송)</summary>
    public List<IDomainEvent> PublicEvents { get; init; } = new();

    /// <summary>Player A 전용 이벤트 (비공개)</summary>
    public List<IDomainEvent> PlayerAPrivateEvents { get; init; } = new();

    /// <summary>Player B 전용 이벤트 (비공개)</summary>
    public List<IDomainEvent> PlayerBPrivateEvents { get; init; } = new();

    public RoundEventCollection(string roomId, int roundNumber)
    {
        RoomId = roomId;
        RoundNumber = roundNumber;
    }

    /// <summary>
    /// 이벤트를 가시성 규칙에 따라 적절한 컬렉션에 분류
    /// </summary>
    public void ClassifyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            // 공개 이벤트
            case GameStarted:
            case RoundStarted:
            case RoundResolved:
            case GameOver:
            case UnitsDeparted:
            case UnitsRetreated:
            case UnitsAdvanced:
            case EncounterOccurred:
            case EncounterResolved:
                PublicEvents.Add(domainEvent);
                break;

            // 비공개 이벤트 (결정자에게만)
            case EncounterDecisionMade decisionMade:
                if (decisionMade.DecidingPlayer == Enums.PlayerSide.A)
                    PlayerAPrivateEvents.Add(domainEvent);
                else
                    PlayerBPrivateEvents.Add(domainEvent);
                break;

            // 제한적 공개 (본부 점유 시에만)
            case UnitsArrived arrived:
                PublicEvents.Add(arrived);  // TODO: 본부 점유 여부에 따른 필터링
                break;

            default:
                PublicEvents.Add(domainEvent);
                break;
        }
    }
}