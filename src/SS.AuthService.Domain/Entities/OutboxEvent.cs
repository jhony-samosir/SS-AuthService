using System;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public class OutboxEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = null!;
    public string AggregateType { get; set; } = null!;
    public int AggregateId { get; set; }
    public string Payload { get; set; } = null!; // Stored as JSON string
    public string Status { get; set; } = "pending";
    public int RetryCount { get; set; } = 0;
    public DateTime? PublishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
