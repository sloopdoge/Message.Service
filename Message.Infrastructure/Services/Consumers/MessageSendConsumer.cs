using MassTransit;
using Message.Domain.Records;
using Message.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services.Consumers;

public class MessageSendConsumer(
    ILogger<MessageSendConsumer> logger,
    IEnumerable<ISenderService> senders,
    IMessageService messageService) : IConsumer<MessageSendRequested>
{
    public async Task Consume(ConsumeContext<MessageSendRequested> context)
    {
        var messageId = context.Message.MessageId;

        try
        {
            if (await messageService.IsProcessed(messageId, context.CancellationToken))
            {
                logger.LogInformation("Message {MessageId} already processed. Skipping.", messageId);
                return;
            }

            var msg = await messageService.GetMessageRecordById(messageId);
            if (msg is null)
            {
                logger.LogWarning("Message {MessageId} not found in DB", messageId);
                return;
            }

            await messageService.MarkAsProcessing(messageId, context.CancellationToken);

            var sender = senders.FirstOrDefault(s => s.Type == msg.SenderType);
            if (sender is null)
                throw new InvalidOperationException($"No sender registered for message type {msg.Type}");

            var success = await sender.Send(msg);
            if (success)
            {
                await messageService.MarkAsSent(msg.Id);
                logger.LogInformation("Message {MessageId} sent successfully to {Recipients}", 
                    msg.Id, string.Join(",", msg.Recipient));
            }
            else
            {
                await messageService.MarkAsFailed(msg.Id, "Sender returned false");
                logger.LogWarning("Message {MessageId} failed to send (sender returned false)", msg.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while processing message {MessageId}", messageId);

            try
            {
                await messageService.MarkAsFailed(messageId, ex.Message);
            }
            catch (Exception markEx)
            {
                logger.LogError(markEx, "Failed to mark message {MessageId} as Failed", messageId);
            }

            throw;
        }
    }
}