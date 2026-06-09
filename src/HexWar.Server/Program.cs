using HexWar.Application.Services;
using HexWar.Application.Sessions;
using HexWar.Infrastructure.Persistence;
using HexWar.Infrastructure.WebSocket;
using HexWar.Matchmaking.Services;
using HexWar.Server.BackgroundServices;
using HexWar.Server.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// 공통 서비스 (WebSocket + gRPC에서 공유)
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSingleton<IGameRoomRepository, InMemoryGameRoomRepository>();
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddSingleton<MatchmakingQueue>();

// 이벤트 브로드캐스터
// SessionRegistry를 팩토리 실행 시 즉시 참조하면 SessionRegistry ↔ IEventBroadcaster 간
// 순환 의존성 오류가 발생하므로 람다 내부에서 지연 참조한다.
builder.Services.AddSingleton<IEventBroadcaster>(sp =>
{
    var connectionManager = sp.GetRequiredService<ConnectionManager>();

    return new InMemoryEventBroadcaster(
        connectionManager,
        roomId =>
        {
            var sessionRegistry = sp.GetRequiredService<SessionRegistry>();
            var session = sessionRegistry.GetSession(roomId);
            return session?.CurrentRound ?? 0;
        });
});

// WebSocket 핸들러
builder.Services.AddSingleton<GameWebSocketHandler>();

// gRPC 매치메이킹 서비스 (싱글톤 등록하여 OnMatchFound 중복 이벤트 구독 방지)
builder.Services.AddSingleton<MatchmakingService>();

// gRPC 서비스
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// 정리 백그라운드 서비스 등록
builder.Services.AddHostedService<SessionCleanupService>();

// 서비스 상태 확인용 HealthCheck
builder.Services.AddControllers();

var app = builder.Build();

// 미들웨어 구성

// HexWar.Client의 wwwroot 경로 설정 (동일 서버에서 클라이언트 서빙)
var clientWebRoot = Path.Combine(app.Environment.ContentRootPath, "..", "HexWar.Client", "wwwroot");

if (Directory.Exists(clientWebRoot))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientWebRoot)
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientWebRoot)
    });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// WebSocket
app.UseWebSockets();
app.UseGameWebSocket();

// gRPC
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.MapGrpcService<MatchmakingService>();
app.MapGrpcService<GameRoomService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapControllers();

app.Run();