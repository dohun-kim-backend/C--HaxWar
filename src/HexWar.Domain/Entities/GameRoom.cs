namespace HexWar.Domain.Entities;

using HexWar.Domain.Enums;
using HexWar.Domain.Events;
using HexWar.Domain.Exceptions;
using HexWar.Domain.ValueObjects;
using HexWar.Domain.Commands;

public class GameRoom
{
    public string RoomId { get; }
    public GamePhase Phase { get; private set; }
    public int CurrentRound { get; private set; }
    public int MaxRounds { get; } = 20;

    // 순차적 접근이 필요하지 않음으로 index 기반의 리스트보다 Dictionary가 더 적합함
    public Dictionary<NodeId, Node> Nodes { get; } = new();
    public Dictionary<EdgeId, Edge> Edges { get; } = new();
    public Dictionary<PlayerSide, PlayerId> Players { get; } = new();

    // 라운드에 소모한 유닛 수 파악용
    public Dictionary<PlayerSide, int> UnitUsedThisRound { get; } = new()
    {
        { PlayerSide.A, 0 },
        { PlayerSide.B, 0 }
    };

    // 최대 이동 가능한 유닛 수
    public const int MaxUnitsPerPlayer = 3;

    // 도메인 이벤트
    public List<IDomainEvent> DomainEvents { get; } = new();

    // 조우 이벤트 목록
    public List<EncounterEvent> PendingEncounters { get; } = new();

    // 생성자 함수를 통한 기본 상태 정의
    public GameRoom(string roomId)
    {
        RoomId = roomId;
        Phase = GamePhase.WatingForPlayers;
    }


    // 게임 설정 단계 정의
    public void InitializeMap()
    {
        // 실제 게임에서는 JSON/설정 파일에서 로드
        CreateNode(new NodeId(1), "Alpha", isHeadquarters: false);
        CreateNode(new NodeId(2), "Beta", isHeadquarters: false);
        CreateNode(new NodeId(3), "Gamma", isHeadquarters: false);
        CreateNode(new NodeId(4), "Delta", isHeadquarters: false);
        CreateNode(new NodeId(5), "Epsilon", isHeadquarters: false);
        CreateNode(new NodeId(6), "HQ", isHeadquarters: true);  // 중앙 본부

        // 간선 생성 (예시 그래프)
        CreateEdge(new NodeId(1), new NodeId(2), new Distance(1));
        CreateEdge(new NodeId(2), new NodeId(3), new Distance(2));
        CreateEdge(new NodeId(3), new NodeId(4), new Distance(1));
        CreateEdge(new NodeId(4), new NodeId(5), new Distance(2));
        CreateEdge(new NodeId(5), new NodeId(1), new Distance(1));
        CreateEdge(new NodeId(6), new NodeId(1), new Distance(1));
        CreateEdge(new NodeId(6), new NodeId(2), new Distance(1));
        CreateEdge(new NodeId(6), new NodeId(3), new Distance(1));
        CreateEdge(new NodeId(6), new NodeId(4), new Distance(1));
        CreateEdge(new NodeId(6), new NodeId(5), new Distance(1));
    }

    public void CreateNode(NodeId id, string name, bool isHeadquarters)
    {
        // 이미 존재하는 노드인지 검사
        if (Nodes.ContainsKey(id))
            throw new InvalidOperationException($"Node with id {id} already exists in the game room.");

        var node = new Node(id, name, isHeadquarters);
        Nodes[id] = node;

    }

    public void CreateEdge(NodeId from, NodeId to, Distance distance)
    {
        var edge = new Edge(from, to, distance);
        Edges[edge.Id] = edge;

        // 이웃 정보 등록
        Nodes[from].Neighbors.Add(to);
        Nodes[to].Neighbors.Add(from);
    }

    // 플레이어 참가 
    public PlayerSide AddPlayer(PlayerId playerId)
    {
        if (Players.Count >= 2)
        {
            throw new DomainException("Room is full");
        }

        if (Players.ContainsValue(playerId))
        {
            throw new DomainException("Player already in room");
        }

        // 세션 내 플레이어 정보 추가
        PlayerSide side = Players.Count == 0 ? PlayerSide.A : PlayerSide.B;
        Players[side] = playerId;

        // 유닛 배치 초기화
        // Player A → Node 1, Player B → Node 5
        NodeId startNode = side == PlayerSide.A ? new NodeId(1) : new NodeId(5);
        Nodes[startNode].StationedUnits[side] = 3;
        Nodes[startNode].Ownership = side == PlayerSide.A
            ? NodeOwnership.PlayerA
            : NodeOwnership.PlayerB;


        // 플레이 모집 여부 확인
        if (Players.Count >= 2)
        {
            Phase = GamePhase.Planning;
            CurrentRound = 1;
            // 이벤트 발생 목록에 추가 
            DomainEvents.Add(new GameStarted(RoomId));
        }

        return side;
    }

    public void MoveUnits(PlayerSide side, MoveCommand command)
    {
        // 계획 단계가 아니라면 움직인 제한
        if (Phase != GamePhase.Planning)
        {
            throw new DomainException($"Cannot move in {Phase} phase");
        }

        // 현재 사용한 유닛 수 초과 여부 확인
        if (UnitUsedThisRound[side] > MaxUnitsPerPlayer)
        {
            throw new DomainException("All 3 units already used this round");
        }

        // 가용 이동 유닛 확인
        int availableUnits = MaxUnitsPerPlayer - UnitUsedThisRound[side];
        int moveCount = Math.Min(command.UnitIds.Count, availableUnits);

        if (moveCount <= 0)
        {
            throw new DomainException("No units available to move");
        }

        // 경로 존재 여부 확인 [ edge 검증 ]
        ValidatePath(command.From, command.To);

        // 노드 변경 사항 진행 
        Nodes[command.From].DepartUnits(side, moveCount);
        UnitUsedThisRound[side] += moveCount;

        // 간선 변경사항 진행
        var edgeId = new EdgeId(command.From, command.To);
        Edges[edgeId].StartTravel(side, moveCount, command.To);

        DomainEvents.Add(new UnitsMoveStarted(RoomId, side, command.From, command.To, moveCount, CurrentRound));
    }

    private void ValidatePath(NodeId from, NodeId to)
    {
        var edgeId = new EdgeId(from, to);
        if (!Edges.ContainsKey(edgeId))
        {
            throw new DomainException($"No path between {from} and {to}");
        }

    }

    // 라운드 내부 명령 순차 소비
    public void ResolveRound()
    {
        if (Phase != GamePhase.Planning)
            throw new DomainException("Cannot resolve round outside Planning phase");

        Phase = GamePhase.Resolution;

        // 1. 도착 병력 정산
        var arrivedUnits = new Dictionary<(NodeId, PlayerSide), int>();

        foreach (var edge in Edges.Values)
        {
            var arrived = edge.AdvanceRound();
            foreach (var group in arrived)
            {
                var key = (group.Destination, group.Side);
                if (!arrivedUnits.ContainsKey(key))
                {
                    arrivedUnits[key] = 0;
                }
                arrivedUnits[key] += group.UnitCount;
            }
        }

        // 2. 조우 이벤트 조회
        var encounters = CheckEncounters();
        PendingEncounters.AddRange(encounters);

        // 3. 조우 관련 이벤트가 처리되어야 하므로 도착 유닛 처리 제외
        var unitsInEncounter = new HashSet<(EdgeId, int, PlayerSide)>();
        foreach (var enc in encounters)
        {
            // TODO 조우 관련 해소 이벤트 정의 필요
        }

        // 4. 노드 별 도착 병력 정리
        foreach (var kvp in arrivedUnits)
        {
            var (nodeId, side) = kvp.Key;
            int count = kvp.Value;
            Nodes[nodeId].ArriveUnits(side, count);
        }

        // 다음 라운드 준비 작업
        CurrentRound++;
        UnitUsedThisRound[PlayerSide.A] = 0;
        UnitUsedThisRound[PlayerSide.B] = 0;

        // 5. 게임 종료 체크
        if (CheckGameOver() || CurrentRound > MaxRounds)
        {
            Phase = GamePhase.GameOver;
            DomainEvents.Add(new GameOver(RoomId, GetWinner()));
        }
        else
        {
            Phase = GamePhase.Planning;
        }

        DomainEvents.Add(new RoundResolved(RoomId, CurrentRound - 1));
    }

    private List<EncounterEvent> CheckEncounters()
    {
        var encounters = new List<EncounterEvent>();

        // 모든 간선 검사를 위한 for
        foreach (var edge in Edges.Values)
        {
            // 간선 내부 유닛들에 따른 조우 이벤트 탐색
            foreach (var kvp in edge.TravelingUnits)
            {
                if (edge.HasEncounter(kvp.Key, out var groups) && groups != null)
                {
                    encounters.Add(new EncounterEvent(
                        edge.Id,
                        groups[0],
                        groups[1],
                        kvp.Key // 남은 라운드
                    ));
                }
            }
        }

        return encounters;
    }

    public void ResolveEncounter(EdgeId edgeId, PlayerSide deciderSide, EncounterDecision decision)
    {
        var encounter = PendingEncounters.FirstOrDefault(e => e.EdgeId == edgeId);
        if (encounter == null)
            throw new DomainException("No pending encounter on this edge");

        // TODO: 결정에 따른 유닛 이동 처리
        // Advance → 전진 계속
        // Retreat → 출발지로 복귀 (다시 Distance만큼 이동)

        PendingEncounters.Remove(encounter);
    }

    // --- 승리 조건 ---

    private bool CheckGameOver()
    {
        // 본부 제외 모든 노드가 한 플레이어 소유인지 확인
        var nonHQNodes = Nodes.Values.Where(n => !n.IsHeadquarters).ToList();
        bool allPlayerA = nonHQNodes.All(n => n.Ownership == NodeOwnership.PlayerA);
        bool allPlayerB = nonHQNodes.All(n => n.Ownership == NodeOwnership.PlayerB);
        return allPlayerA || allPlayerB;
    }

    private PlayerSide? GetWinner()
    {
        int nodesA = Nodes.Values.Count(n => n.Ownership == NodeOwnership.PlayerA);
        int nodesB = Nodes.Values.Count(n => n.Ownership == NodeOwnership.PlayerB);

        if (nodesA > nodesB) return PlayerSide.A;
        if (nodesB > nodesA) return PlayerSide.B;
        return null; // 무승부
    }
}