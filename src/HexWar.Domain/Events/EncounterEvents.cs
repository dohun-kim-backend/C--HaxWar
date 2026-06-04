namespace HexWar.Domain.Events;

using HexWar.Domain.Enums;
using HexWar.Domain.ValueObjects;

/// <summary>
/// 조우 발생 이벤트 (공개)
/// 같은 간선에서 양측 유닛이 마주친 경우 발생
/// </summary>
public sealed record EncounterOccurred : DomainEvent
{
    /// <summary>조우가 발생한 간선 ID</summary>
    public EdgeId EdgeId { get; init; }

    /// <summary>간선의 시작 노드</summary>
    public NodeId FromNode { get; init; }

    /// <summary>간선의 도착 노드</summary>
    public NodeId ToNode { get; init; }

    /// <summary>조우 발생 시 남은 라운드</summary>
    public int RemainingRounds { get; init; }

    /// <summary>Player A의 조우 유닛 정보</summary>
    public EncounterParticipantInfo ParticipantA { get; init; }

    /// <summary>Player B의 조우 유닛 정보</summary>
    public EncounterParticipantInfo ParticipantB { get; init; }

    /// <summary>조우가 발생한 라운드</summary>
    public int RoundNumber { get; init; }

    public EncounterOccurred(
        string roomId,
        EdgeId edgeId,
        NodeId fromNode,
        NodeId toNode,
        int remainingRounds,
        EncounterParticipantInfo participantA,
        EncounterParticipantInfo participantB,
        int roundNumber)
        : base(roomId)
    {
        EdgeId = edgeId;
        FromNode = fromNode;
        ToNode = toNode;
        RemainingRounds = remainingRounds;
        ParticipantA = participantA;
        ParticipantB = participantB;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// 조우 참여자 정보
/// </summary>
public sealed record EncounterParticipantInfo
{
    /// <summary>플레이어 측</summary>
    public PlayerSide Side { get; init; }

    /// <summary>참여 중인 유닛 수</summary>
    public int UnitCount { get; init; }

    public EncounterParticipantInfo(PlayerSide side, int unitCount)
    {
        Side = side;
        UnitCount = unitCount;
    }
}

/// <summary>
/// 조우 결정 이벤트 (비공개)
/// 개별 플레이어가 전진/복귀 결정을 내린 경우 발생
/// 상대방에게는 공개되지 않음
/// </summary>
public sealed record EncounterDecisionMade : DomainEvent
{
    /// <summary>결정을 내린 플레이어</summary>
    public PlayerSide DecidingPlayer { get; init; }

    /// <summary>조우가 발생한 간선</summary>
    public EdgeId EdgeId { get; init; }

    /// <summary>내려진 결정</summary>
    public EncounterDecision Decision { get; init; }

    /// <summary>결정 대상 유닛 수</summary>
    public int UnitCount { get; init; }

    /// <summary>결정이 내려진 라운드</summary>
    public int RoundNumber { get; init; }

    public EncounterDecisionMade(
        string roomId,
        PlayerSide decidingPlayer,
        EdgeId edgeId,
        EncounterDecision decision,
        int unitCount,
        int roundNumber)
        : base(roomId)
    {
        DecidingPlayer = decidingPlayer;
        EdgeId = edgeId;
        Decision = decision;
        UnitCount = unitCount;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// 조우 해소 완료 이벤트 (공개)
/// 양측 모두 결정을 내려 조우가 완전히 해소된 경우 발생
/// </summary>
public sealed record EncounterResolved : DomainEvent
{
    /// <summary>조우가 발생한 간선</summary>
    public EdgeId EdgeId { get; init; }

    /// <summary>Player A의 최종 결정</summary>
    public EncounterDecision DecisionA { get; init; }

    /// <summary>Player B의 최종 결정</summary>
    public EncounterDecision DecisionB { get; init; }

    /// <summary>해소 후 Player A 유닛의 행방</summary>
    public EncounterOutcome OutcomeA { get; init; }

    /// <summary>해소 후 Player B 유닛의 행방</summary>
    public EncounterOutcome OutcomeB { get; init; }

    /// <summary>해소된 라운드</summary>
    public int RoundNumber { get; init; }

    public EncounterResolved(
        string roomId,
        EdgeId edgeId,
        EncounterDecision decisionA,
        EncounterDecision decisionB,
        EncounterOutcome outcomeA,
        EncounterOutcome outcomeB,
        int roundNumber)
        : base(roomId)
    {
        EdgeId = edgeId;
        DecisionA = decisionA;
        DecisionB = decisionB;
        OutcomeA = outcomeA;
        OutcomeB = outcomeB;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// 조우 해소 후 개별 유닛의 결과
/// </summary>
public sealed record EncounterOutcome
{
    /// <summary>해당 플레이어</summary>
    public PlayerSide Side { get; init; }

    /// <summary>유닛이 향하는 목적지 (전진 시 원래 목적지, 복귀 시 출발지)</summary>
    public NodeId DestinationNode { get; init; }

    /// <summary>이동 중인 유닛 수</summary>
    public int UnitCount { get; init; }

    /// <summary>도착까지 남은 라운드 (복귀 시 거리만큼 재설정)</summary>
    public int RemainingRounds { get; init; }

    public EncounterOutcome(PlayerSide side, NodeId destinationNode, int unitCount, int remainingRounds)
    {
        Side = side;
        DestinationNode = destinationNode;
        UnitCount = unitCount;
        RemainingRounds = remainingRounds;
    }
}