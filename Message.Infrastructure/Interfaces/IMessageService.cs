using Message.Domain;
using Message.Domain.Dtos;

namespace Message.Infrastructure.Interfaces;

public interface IMessageService
{
    Task<ResponseModel<bool>> SendMessage(SendMessageDto model);
    
    Task<Message.Domain.Entities.Message?> SaveMessageRecord(Message.Domain.Entities.Message message);
    Task<Message.Domain.Entities.Message?> GetMessageRecordById(Guid messageId);
    
    Task<List<Message.Domain.Entities.Message>> GetPending(CancellationToken ct = default);
    Task<List<Message.Domain.Entities.Message>> GetFailed(CancellationToken ct = default);
    Task<List<Message.Domain.Entities.Message>> GetScheduled(CancellationToken ct = default);
    
    Task<bool> IsProcessed(Guid messageId, CancellationToken ct = default);
    
    Task MarkAsProcessing(Guid messageId, CancellationToken ct = default);
    Task MarkAsSent(Guid messageId);
    Task MarkAsFailed(Guid messageId, string reason);
    Task MarkAsDelayed(Guid messageId, TimeSpan delay, CancellationToken ct = default);
}