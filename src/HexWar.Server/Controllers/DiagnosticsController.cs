namespace HexWar.Server.Controllers;

using System.Diagnostics;
using HexWar.Application.Sessions;
using HexWar.Infrastructure.WebSocket;
using HexWar.Server.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly SessionRegistry _sessionRegistry;
    private readonly ConnectionManager _connectionManager;

    public DiagnosticsController(SessionRegistry sessionRegistry, ConnectionManager connectionManager)
    {
        _sessionRegistry = sessionRegistry;
        _connectionManager = connectionManager;
    }

    [HttpGet("stats")]
    public ActionResult<ServerStats> GetStats()
    {
        var process = Process.GetCurrentProcess();
        var sessions = _sessionRegistry.GetActiveSessions();

        var stats = new ServerStats
        {
            Timestamp = DateTime.UtcNow,

            // 프로세스 메모리
            WorkingSetMB = process.WorkingSet64 / 1024.0 / 1024.0,
            PrivateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0,

            // GC 메모리
            GCHeapMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
            GCGen0 = GC.CollectionCount(0),
            GCGen1 = GC.CollectionCount(1),
            GCGen2 = GC.CollectionCount(2),

            // 세션 정보
            TotalSessions = sessions.Count,
            ActiveSessions = sessions.Count(s => s.CurrentPhase == Domain.Enums.GamePhase.Planning),
            GameOverSessions = sessions.Count(s => s.CurrentPhase == Domain.Enums.GamePhase.GameOver),

            // 연결 정보
            TotalConnections = _connectionManager.GetTotalConnectionCount(),

            // 평균 세션당 메모리 (추정)
            EstimatedMemoryPerSessionKB = sessions.Any()
                ? (GC.GetTotalMemory(false) / 1024.0) / sessions.Count
                : 0,
        };

        return Ok(stats);
    }
}