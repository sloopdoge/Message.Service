using Message.Domain.Enums;

namespace Message.Domain.Dtos;

public class SendMessageDto
{
    public List<string> Recipient { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; }
    
    public MessageType MessageType { get; set; }
    public DateTime? SendAt { get; set; }
    
    public SenderType SenderType { get; set; }
}