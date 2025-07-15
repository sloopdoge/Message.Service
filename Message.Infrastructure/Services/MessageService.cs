using Message.Domain;
using Message.Domain.Dtos;
using Message.Domain.Enums;
using Message.Domain.Exceptions;
using Message.Infrastructure.Interfaces;
using Message.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services;

public class MessageService(
    ILogger<MessageService> logger,
    ApplicationRepository repository) : IMessageService
{
    public async Task<ResponseModel<bool>> SendMessage(SendMessageDto model)
    {
        var response = new ResponseModel<bool>
        {
            Succeeded = false,
            Value = false
        };

        try
        {
            var newMessage = await SaveMessageRecord(new Domain.Entities.Message(model));
            if (newMessage is null)
                throw new Exception($"There was error saving the message record for recipient: {model.Recipient}");

            response.Succeeded = true;
            response.Value = true;
            response.Error = null;

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);

            response.Error = new Error { Property = $"{nameof(SendMessage)}", Description = [e.Message] };
            return response;
        }
    }

    public async Task<ResponseModel<bool>> SendMessageWithResponse(SendMessageDto model)
    {
        var response = new ResponseModel<bool>
        {
            Succeeded = false,
            Value = false
        };

        try
        {
            var newMessage = await SaveMessageRecord(new Domain.Entities.Message(model));
            if (newMessage is null)
                throw new Exception($"There was error saving the message record for recipient: {model.Recipient}");

            response.Succeeded = true;
            response.Value = true;
            response.Error = null;

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);

            response.Error = new Error { Property = $"{nameof(SendMessage)}", Description = [e.Message] };
            return response;
        }
    }

    public async Task<Domain.Entities.Message?> SaveMessageRecord(Domain.Entities.Message message)
    {
        try
        {
            var result = repository.Messages.Add(message);
            var saveResult = await repository.SaveChangesAsync();
            return saveResult > 0
                ? result.Entity
                : null;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return null;
        }
    }

    public Task<Domain.Entities.Message?> GetMessageRecordById(Guid messageId) => repository.Messages
        .FirstOrDefaultAsync(m => m.Id == messageId);

    public Task<List<Domain.Entities.Message>> GetPending() => repository.Messages
        .Where(m => m.Status == MessageStatus.Pending)
        .ToListAsync();

    public Task<List<Domain.Entities.Message>> GetFailed() => repository.Messages
        .Where(m => m.Status == MessageStatus.Failed && m.RetryCount < 4)
        .ToListAsync();

    public async Task MarkAsSent(Guid messageId)
    {
        try
        {
            var message = await repository.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message is null)
                throw new MessageNotFoundException(messageId);

            message.Status = MessageStatus.Sent;
            await repository.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    public async Task MarkAsFailed(Guid messageId, string reason)
    {
        try
        {
            var message = await repository.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message is null)
                throw new MessageNotFoundException(messageId);

            message.RetryCount++;
            message.Status = MessageStatus.Failed;
            message.ErrorMessage = reason;
            await repository.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}