namespace HexWar.Domain.Entities;

using HexWar.Domain.Enums;
using HexWar.Domain.ValueObjects;
using HexWar.Domain.Events;

public record MoveResult(int ActualMoved, NodeId From, NodeId To);

public record ArrivalInfo(NodeId Destination, PlayerSide Side, int Count, EdgeId ViaEdge);

public class RoundResolutionResult
{
    public List<ArrivalInfo> ArrivedUnits { get; } = new();
    public List<EncounterOccurred> Encounters { get; } = new();
    public List<PendingEncounter> PendingEncounters { get; } = new();
    public bool GameOver { get; set; }
    public PlayerSide? Winner { get; set; }
}

public class PendingEncounter
{
    public EdgeId EdgeId { get; }
    public TravelingGroup GroupA { get; }
    public TravelingGroup GroupB { get; }
    public int RemainingRounds { get; }

    public EncounterDecision? DecisionA { get; private set; }
    public EncounterDecision? DecisionB { get; private set; }
    public EncounterOutcome? OutcomeA { get; private set; }
    public EncounterOutcome? OutcomeB { get; private set; }

    public bool BothDecided => DecisionA.HasValue && DecisionB.HasValue;

    public PendingEncounter(EdgeId edgeId, TravelingGroup groupA, TravelingGroup groupB, int remainingRounds)
    {
        EdgeId = edgeId;
        GroupA = groupA;
        GroupB = groupB;
        RemainingRounds = remainingRounds;
    }

    public void MarkDecided(PlayerSide side, EncounterDecision decision, EncounterOutcome outcome)
    {
        if (side == PlayerSide.A)
        {
            DecisionA = decision;
            OutcomeA = outcome;
        }
        else
        {
            DecisionB = decision;
            OutcomeB = outcome;
        }
    }
}