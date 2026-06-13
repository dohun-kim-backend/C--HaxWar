namespace HexWar.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using HexWar.Domain.Entities;
using HexWar.Domain.Enums;
using HexWar.Domain.ValueObjects;

using HexWar.Domain.Commands;
using HexWar.Domain.Exceptions;

// 
[MemoryDiagnoser]        // 할당량 측정
[GcServer(true)]         // Server GC 모드
[GcConcurrent(true)]     // Concurrent GC
[GcForce(true)]          // 각 반복 전 GC 강제
public class GameRoomBenchmarks
{
    private GameRoom _gameRoom = null!;

    /// <summary>
    /// 실제 게임 흐름을 시뮬레이션합니다.
    /// 유닛이 고갈되지 않도록 이동 패턴을 조정합니다.
    /// </summary>
    private void SimulateGame(GameRoom gameRoom, int roundCount)
    {
        var random = new Random(42); // 결정적 시드

        for (int round = 0; round < roundCount; round++)
        {
            if (gameRoom.Phase == GamePhase.GameOver) break;

            // Player A의 이동
            SimulatePlayerMoves(gameRoom, PlayerSide.A, random);

            // Player B의 이동
            SimulatePlayerMoves(gameRoom, PlayerSide.B, random);

            // 라운드 해소
            if (gameRoom.Phase == GamePhase.Planning)
            {
                gameRoom.ResolveRound();
            }
        }
    }

    /// <summary>
    /// 한 플레이어의 이동을 시뮬레이션합니다.
    /// 유닛이 있는 노드에서만 이동을 시도합니다.
    /// </summary>
    private void SimulatePlayerMoves(GameRoom gameRoom, PlayerSide side, Random random)
    {
        int unitsToMove = random.Next(0, 4); // 0~3기 이동

        for (int i = 0; i < unitsToMove; i++)
        {
            // 이동 가능한 유닛이 있는 노드 찾기
            var availableNodes = gameRoom.Nodes.Values
                .Where(n => !n.IsHeadquarters) // 본부에서는 출발 불가 (본부 점령 불가)
                .Where(n => n.GetMobileCount(side) > 0)
                .ToList();

            if (!availableNodes.Any()) break;

            // 랜덤하게 출발 노드 선택
            var fromNode = availableNodes[random.Next(availableNodes.Count)];

            // 이웃 노드 중에서 랜덤하게 목적지 선택
            var neighbors = fromNode.Neighbors
                .Where(n => !gameRoom.Nodes[n].IsHeadquarters) // 본부로 이동 가능
                .ToList();

            if (!neighbors.Any()) continue;

            var toNode = neighbors[random.Next(neighbors.Count)];

            try
            {
                gameRoom.MoveUnits(side, new MoveCommand(
                    fromNode.Id, toNode, 1));
            }
            catch (DomainException)
            {
                // 이동 불가능하면 건너뜀
            }
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _gameRoom = new GameRoom("benchmark");
        _gameRoom.InitializeMap();
        _gameRoom.AddPlayer(new PlayerId("p1"));
        _gameRoom.AddPlayer(new PlayerId("p2"));
    }


    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public void SimulateRounds(int roundCount)
    {
        var room = new GameRoom("benchmark-sim");
        room.InitializeMap();
        room.AddPlayer(new PlayerId("p1"));
        room.AddPlayer(new PlayerId("p2"));
        SimulateGame(room, roundCount);
    }

    [Benchmark]
    public GameRoom CreateAndInitialize()
    {
        var room = new GameRoom("new-room");
        room.InitializeMap();
        room.AddPlayer(new PlayerId("p1"));
        room.AddPlayer(new PlayerId("p2"));
        return room;
    }
}