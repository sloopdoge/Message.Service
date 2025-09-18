using Message.Domain.Dtos;
using Message.Domain.Enums;

namespace Message.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public List<string> Recipient { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; }

    public SenderType SenderType { get; set; }
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; } 
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public Message() { }
    
    public Message(SendMessageDto  model)
    {
        Id = Guid.NewGuid();
        
        Recipient = model.Recipient;
        Subject = model.Subject;
        Body = model.Body;
        SenderType = model.SenderType;
        
        Type = model.MessageType;
        ScheduledAt = model.SendAt;

        Status = MessageStatus.Pending;
        
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }
}
