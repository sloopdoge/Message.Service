using Message.Domain.Enums;

namespace Message.Infrastructure.Interfaces;

public interface ISenderService
{
    SenderType Type { get; }
    Task<bool> Send(Domain.Entities.Message message);
}