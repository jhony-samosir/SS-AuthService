using System;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public class InboxEvent
{
    public string MessageId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? AggregateType { get; set; }
    public string Payload { get; set; } = null!; // Stored as JSON string
    public string Status { get; set; } = "processed";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
