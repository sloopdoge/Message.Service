using Message.Domain;
using Message.Domain.Dtos;
using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.Exceptions;
using Message.Domain.Records;
using Message.Infrastructure.Interfaces;
using Message.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Message.Infrastructure.Services;

public class MessageService(
    ILogger<MessageService> logger,
    ApplicationRepository repository) : IMessageService
{
    #region Create / Save

    public async Task<ResponseModel<bool>> SendMessage(SendMessageDto model)
    {
        var response = new ResponseModel<bool> { Succeeded = false, Value = false };

        try
        {
            var newMessage = await SaveMessageRecord(new Message.Domain.Entities.Message(model));
            if (newMessage is null)
                throw new Exception($"Error saving message record for recipient: {string.Join(",", model.Recipient)}");
            
            var outboxEvent = new OutboxMessage
            {
                Type = nameof(MessageSendRequested),
                Payload = JsonConvert.SerializeObject(new MessageSendRequested(newMessage.Id)),
                ScheduledAt = model.SendAt
            };
            repository.OutboxMessages.Add(outboxEvent);
            await repository.SaveChangesAsync();

            response.Succeeded = true;
            response.Value = true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in {Method}", nameof(SendMessage));
            response.Error = new Error { Property = nameof(SendMessage), Description = [e.Message] };
        }

        return response;
    }

    public async Task<Message.Domain.Entities.Message?> SaveMessageRecord(Message.Domain.Entities.Message message)
    {
        try
        {
            var result = repository.Messages.Add(message);
            var saveResult = await repository.SaveChangesAsync();
            return saveResult > 0 ? result.Entity : null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving message record {MessageId}", message.Id);
            return null;
        }
    }

    #endregion

    #region Queries

    public Task<Message.Domain.Entities.Message?> GetMessageRecordById(Guid messageId) =>
        repository.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

    public Task<List<Message.Domain.Entities.Message>> GetPending(CancellationToken ct = default) =>
        repository.Messages
            .Where(m => m.Status == MessageStatus.Pending)
            .ToListAsync(cancellationToken: ct);

    public Task<List<Message.Domain.Entities.Message>> GetFailed(CancellationToken ct = default) =>
        repository.Messages
            .Where(m => m.Status == MessageStatus.Failed && m.RetryCount < 4)
            .ToListAsync(cancellationToken: ct);

    public Task<List<Message.Domain.Entities.Message>> GetScheduled(CancellationToken ct = default) =>
        repository.Messages
            .Where(m => m.Status == MessageStatus.Delayed && m.ScheduledAt <= DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task<bool> IsProcessed(Guid messageId, CancellationToken ct = default)
    {
        var msg = await repository.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId, ct);

        return msg is not null &&
               (msg.Status == MessageStatus.Sent || msg.Status == MessageStatus.Failed);
    }

    #endregion

    #region State Updates

    public async Task MarkAsProcessing(Guid messageId, CancellationToken ct = default)
    {
        var msg = await repository.Messages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null)
            throw new MessageNotFoundException(messageId);

        msg.Status = MessageStatus.Processing;
        msg.ProcessedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
    }

    public async Task MarkAsSent(Guid messageId)
    {
        var msg = await repository.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (msg is null)
            throw new MessageNotFoundException(messageId);

        msg.Status = MessageStatus.Sent;
        msg.SentAt = DateTime.UtcNow;
        
        await repository.SaveChangesAsync();
    }

    public async Task MarkAsFailed(Guid messageId, string reason)
    {
        var msg = await repository.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (msg is null)
            throw new MessageNotFoundException(messageId);

        msg.RetryCount++;
        msg.Status = MessageStatus.Failed;
        msg.ErrorMessage = reason;
        await repository.SaveChangesAsync();
    }

    public async Task MarkAsDelayed(Guid messageId, TimeSpan delay, CancellationToken ct = default)
    {
        var msg = await repository.Messages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg is null)
            throw new MessageNotFoundException(messageId);

        msg.Status = MessageStatus.Delayed;
        msg.ScheduledAt = DateTime.UtcNow.Add(delay);
        await repository.SaveChangesAsync(ct);
    }

    #endregion
}