namespace HexWar.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using HexWar.Domain.Commands;
using HexWar.Domain.Entities;
using HexWar.Domain.Enums;
using HexWar.Domain.Exceptions;
using HexWar.Domain.ValueObjects;

// 벤치마크 전체 실행 명령어: dotnet run -c Release --project tests/HexWar.Benchmarks
// 특정 클래스 필터 실행 명령어: dotnet run -c Release --project tests/HexWar.Benchmarks -- --filter *GameRoomBenchmarks*

// 어노테이션 의미
[MemoryDiagnoser]        // 할당량 측정
[GcServer(true)]         // Server GC 모드
[GcConcurrent(true)]     // Concurrent GC
[GcForce(true)]          // 각 반복 전 GC 강제
public class GameRoomBenchmarks
{
    // 게임 시뮬레이터를 위한 필드 내부
    private RealisticGameSimulator _simulator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simulator = new RealisticGameSimulator(seed: 42);
    }


    /// <summary>
    /// GameRoom 객체 생성 시간 측정 
    /// </summary>
    [Benchmark]
    public GameRoom CreateAndInitialize()
    {
        var room = new GameRoom("new-room");
        room.InitializeMap();
        room.AddPlayer(new PlayerId("p1"));
        room.AddPlayer(new PlayerId("p2"));
        return room;
    }

    /// <summary>
    /// 1회 완전 게임 시뮬레이션의 메모리/시간 측정
    /// </summary>
    [Benchmark]
    public GameSimulationResult SimulateOneCompleteGame()
    {
        return _simulator.SimulateCompleteGame();
    }

    /// <summary>
    /// 단계적 연속 게임 시뮬레이션
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void SimulateMultipleGamesPure(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _simulator.SimulateCompleteGame();
        }
    }


}