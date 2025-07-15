using Message.Domain;
using Message.Domain.Dtos;

namespace Message.Infrastructure.Interfaces;

public interface IMessageService
{
    Task<ResponseModel<bool>> SendMessage(SendMessageDto model);
    Task<Domain.Entities.Message?> SaveMessageRecord(Domain.Entities.Message message);
    Task<Domain.Entities.Message?> GetMessageRecordById(Guid messageId);
    Task<List<Domain.Entities.Message>> GetPending();
    Task<List<Domain.Entities.Message>> GetFailed();
    Task MarkAsSent(Guid messageId);
    Task MarkAsFailed(Guid messageId, string reason);
}