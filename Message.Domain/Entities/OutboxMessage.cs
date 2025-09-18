namespace Message.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? Headers { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
}