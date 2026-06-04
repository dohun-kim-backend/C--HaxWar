namespace HexWar.Domain.Entities;

using HexWar.Domain.Commands;
using HexWar.Domain.Enums;
using HexWar.Domain.Events;
using HexWar.Domain.Exceptions;
using HexWar.Domain.ValueObjects;

public partial class GameRoom
{
    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private List<EncounterOccurred> _encounterEvents = new();
    private List<NodeOwnershipChanged> _ownershipChanges = new();
    private List<ArrivalRecord> _arrivalRecords = new();

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    private void ClearEvents()
    {
        _domainEvents.Clear();
        _encounterEvents.Clear();
        _ownershipChanges.Clear();
        _arrivalRecords.Clear();
    }

    private void RecordEncounter(EncounterOccurred encounter)
    {
        _encounterEvents.Add(encounter);
        RaiseEvent(encounter);
    }

    private void RecordOwnershipChange(NodeId nodeId, NodeOwnership previous, NodeOwnership current, bool isSupplyLine)
    {
        var change = new NodeOwnershipChanged(nodeId, previous, current, isSupplyLine);
        _ownershipChanges.Add(change);
    }

    private void RecordArrival(NodeId destination, PlayerSide side, int count, EdgeId viaEdge)
    {
        _arrivalRecords.Add(new ArrivalRecord(destination, side, count, viaEdge));
    }

    // 수정된 ResolveRound 메서드
    public RoundResolutionResult ResolveRound()
    {
        if (Phase != GamePhase.Planning)
            throw new DomainException("Cannot resolve round outside Planning phase");

        Phase = GamePhase.Resolution;
        ClearEvents();
        var result = new RoundResolutionResult();

        // 1. 모든 간선에서 1라운드 진행 → 도착 유닛 수집
        var arrivals = ProcessAllEdgeAdvances();

        // 2. 조우 확인
        var encounters = FindAllEncounters();

        // 3. 조우 이벤트 기록
        foreach (var enc in encounters)
        {
            var edge = Edges[enc.EdgeId];
            var encounterEvent = new EncounterOccurred(
                RoomId,
                enc.EdgeId,
                edge.From,
                edge.To,
                enc.RemainingRounds,
                new EncounterParticipantInfo(enc.GroupA.Side, enc.GroupA.UnitCount),
                new EncounterParticipantInfo(enc.GroupB.Side, enc.GroupB.UnitCount),
                CurrentRound
            );
            RecordEncounter(encounterEvent);
            result.Encounters.Add(encounterEvent);
        }

        // 4. 조우 중인 유닛 식별
        var blockedUnits = GetBlockedUnits(encounters);

        // 5. 조우 없는 유닛 도착 처리
        foreach (var arrival in arrivals)
        {
            if (!blockedUnits.Contains(arrival))
            {
                var node = Nodes[arrival.Destination];
                var previousOwnership = node.Ownership;

                node.ArriveMobileUnits(arrival.Side, arrival.Count);

                RecordArrival(arrival.Destination, arrival.Side, arrival.Count, arrival.ViaEdge);

                if (node.Ownership != previousOwnership)
                {
                    RecordOwnershipChange(
                        arrival.Destination,
                        previousOwnership,
                        node.Ownership,
                        node.IsSupplyLine
                    );
                }

                RaiseEvent(new UnitsArrived(
                    RoomId,
                    arrival.Destination,
                    arrival.Side,
                    arrival.Count,
                    arrival.ViaEdge,
                    CurrentRound,
                    node.Ownership
                ));

                result.ArrivedUnits.Add(arrival);
            }
        }

        // 6. 조우 Pending 등록
        foreach (var encEvent in _encounterEvents)
        {
            var edge = Edges[encEvent.EdgeId];
            var travelingGroups = edge.TravelingUnits[encEvent.RemainingRounds];

            var groupA = travelingGroups.First(g => g.Side == PlayerSide.A);
            var groupB = travelingGroups.First(g => g.Side == PlayerSide.B);

            var pending = new PendingEncounter(
                encEvent.EdgeId,
                groupA,
                groupB,
                encEvent.RemainingRounds
            );
            PendingEncounters.Add(pending);
            result.PendingEncounters.Add(pending);
        }

        // 7. 노드 출발 기록 초기화
        foreach (var node in Nodes.Values)
            node.ClearDepartureHistory();

        // 8. 라운드 카운터 증가
        int completedRound = CurrentRound;
        CurrentRound++;
        UnitUsedThisRound[PlayerSide.A] = 0;
        UnitUsedThisRound[PlayerSide.B] = 0;

        // 9. 라운드 해소 이벤트 발행
        RaiseEvent(new RoundResolved(
            RoomId,
            completedRound,
            _arrivalRecords,
            _encounterEvents,
            _ownershipChanges
        ));

        // 10. 게임 종료 체크
        if (CheckGameOver() || CurrentRound > MaxRounds)
        {
            Phase = GamePhase.GameOver;
            var winner = GetWinner();
            var reason = CurrentRound > MaxRounds
                ? GameOverReason.MaxRoundsReached
                : GameOverReason.AllNodesCaptured;

            var scores = new Dictionary<PlayerSide, int>
            {
                { PlayerSide.A, Nodes.Values.Count(n => n.Ownership == NodeOwnership.PlayerA) },
                { PlayerSide.B, Nodes.Values.Count(n => n.Ownership == NodeOwnership.PlayerB) }
            };

            RaiseEvent(new GameOver(RoomId, winner, reason, completedRound, scores));

            result.GameOver = true;
            result.Winner = winner;
        }
        else
        {
            Phase = GamePhase.Planning;
            RaiseEvent(new RoundStarted(RoomId, CurrentRound));
            result.GameOver = false;
        }

        return result;
    }

    // 수정된 MoveUnits 메서드
    public MoveResult MoveUnits(PlayerSide side, MoveCommand command)
    {
        if (Phase != GamePhase.Planning)
            throw new DomainException($"Cannot move in {Phase} phase");

        if (UnitUsedThisRound[side] >= MaxUnitsPerPlayer)
            throw new DomainException("All units already moved this round");

        int requested = Math.Min(command.UnitCount, MaxUnitsPerPlayer - UnitUsedThisRound[side]);

        ValidatePath(command.From, command.To);

        var sourceNode = Nodes[command.From];
        int actualMoved = sourceNode.DepartMobileUnits(side, requested);

        UnitUsedThisRound[side] += actualMoved;

        var edgeId = new EdgeId(command.From, command.To);
        Edges[edgeId].StartTravel(side, actualMoved, command.To);

        // 출발 이벤트 (공개 정보만)
        RaiseEvent(new UnitsDeparted(RoomId, side, command.From, actualMoved, CurrentRound));

        return new MoveResult(actualMoved, command.From, command.To);
    }

    // 수정된 ResolveEncounter 메서드
    public void ResolveEncounter(EdgeId edgeId, PlayerSide deciderSide, EncounterDecision decision)
    {
        var pending = PendingEncounters.FirstOrDefault(e => e.EdgeId == edgeId);
        if (pending == null)
            throw new DomainException("No pending encounter on this edge");

        var edge = Edges[edgeId];
        var decidingGroup = pending.GroupA.Side == deciderSide ? pending.GroupA : pending.GroupB;
        var otherGroup = pending.GroupA.Side == deciderSide ? pending.GroupB : pending.GroupA;

        // 결정 이벤트 발행 (비공개)
        RaiseEvent(new EncounterDecisionMade(
            RoomId,
            deciderSide,
            edgeId,
            decision,
            decidingGroup.UnitCount,
            CurrentRound
        ));

        // 결정에 따른 처리
        EncounterOutcome outcome;
        if (decision == EncounterDecision.Retreat)
        {
            NodeId retreatDestination = GetRetreatDestination(edge, deciderSide);
            edge.StartTravel(deciderSide, decidingGroup.UnitCount, retreatDestination);

            outcome = new EncounterOutcome(
                deciderSide,
                retreatDestination,
                decidingGroup.UnitCount,
                edge.Distance.RoundsRequired  // 복귀 시 원래 거리만큼 다시 이동
            );

            RaiseEvent(new UnitsRetreated(RoomId, deciderSide, edgeId, decidingGroup.UnitCount, CurrentRound));
        }
        else
        {
            outcome = new EncounterOutcome(
                deciderSide,
                decidingGroup.Destination,
                decidingGroup.UnitCount,
                pending.RemainingRounds  // 전진 시 남은 라운드 유지
            );

            RaiseEvent(new UnitsAdvanced(RoomId, deciderSide, edgeId, decidingGroup.UnitCount, CurrentRound));
        }

        // 결정 등록
        pending.MarkDecided(deciderSide, decision, outcome);

        // 양측 모두 결정 완료 시 해소 이벤트 발행
        if (pending.BothDecided)
        {
            PendingEncounters.Remove(pending);

            RaiseEvent(new EncounterResolved(
                RoomId,
                edgeId,
                pending.DecisionA!.Value,
                pending.DecisionB!.Value,
                pending.OutcomeA!,
                pending.OutcomeB!,
                CurrentRound
            ));
        }
    }

    private NodeId GetRetreatDestination(Edge edge, PlayerSide side)
    {
        // TravelingGroup에 출발지 정보가 없으므로,
        // 간선의 양 끝 노드 중 해당 플레이어가 더 많은 유닛을 보유한 곳으로 추정
        var nodeFrom = Nodes[edge.From];
        var nodeTo = Nodes[edge.To];

        // 더 많은 유닛이 있는 노드가 출발지일 가능성이 높음
        if (nodeFrom.GetTotalCount(side) >= nodeTo.GetTotalCount(side))
            return edge.From;
        else
            return edge.To;
    }
}