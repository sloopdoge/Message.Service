using Message.Domain.Enums;

namespace Message.Domain.Dtos;

public class SendMessageDto
{
    public List<string> Recipient { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; }
    public MessageType Type { get; set; }
}