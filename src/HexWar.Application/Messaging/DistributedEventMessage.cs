using System;
using System.Text.Json;

namespace HexWar.Application.Messaging;

/// <summary>
/// Redis Pub/Sub으로 전달되는 이벤트 메시지 DTO
/// </summary>
public class DistributedEventMessage
{
    public string RoomId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public JsonElement EventData { get; set; }
    public long SequenceNumber { get; set; }
    public string SourceServerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
