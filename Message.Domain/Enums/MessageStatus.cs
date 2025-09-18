namespace Message.Domain.Enums;

public enum MessageStatus
{
    Pending,
    Processing,
    Delayed,
    Sent,
    Failed,
}