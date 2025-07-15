using Message.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services.Workers;

public class MessageSenderWorkerService(
    ILogger<MessageSenderWorkerService> logger,
    IServiceProvider serviceProvider) : IHostedService
{
    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = SenderProcess(CancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await CancellationTokenSource.CancelAsync();
    }

    private async Task SenderProcess(CancellationToken cancellationToken)
    {
        try
        {
            await using var asyncServiceScope = serviceProvider.CreateAsyncScope();
            var messageService = asyncServiceScope.ServiceProvider
                .GetRequiredService<IMessageService>();
            var senderServices = asyncServiceScope.ServiceProvider
                .GetServices<ISenderService>().ToList();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var pendingMessages = await messageService.GetPending();
                    var messagesSentCount = pendingMessages.Count;
                    
                    if (pendingMessages.Any())
                    {
                        var messageQueue =
                            new Queue<Domain.Entities.Message>(pendingMessages.OrderBy(m => m.CreatedAt));
                        await ProcessMessageQueue(cancellationToken, messageQueue, senderServices, messageService);
                        messagesSentCount -= messageQueue.Count;
                        
                        logger.LogInformation($"Successfully sent {messagesSentCount} messages.");
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unexpected error in SenderProcess loop");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Fatal error in SenderProcess");
        }
    }

    private async Task ProcessMessageQueue(
        CancellationToken cancellationToken,
        Queue<Domain.Entities.Message> messages,
        List<ISenderService> senderServices,
        IMessageService messageService)
    {
        while (!cancellationToken.IsCancellationRequested && messages.TryDequeue(out var message))
        {
            try
            {
                var senderService = senderServices.FirstOrDefault(ss => ss.Type == message.Type);
                if (senderService is null)
                    throw new Exception($"No sender found for MessageType {message.Type}");

                var success = await senderService.Send(message);
                if (success)
                    await messageService.MarkAsSent(message.Id);
                else
                    await messageService.MarkAsFailed(message.Id, "Unknown sending error");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process message {MessageId}", message.Id);
                await messageService.MarkAsFailed(message.Id, e.Message);
            }
        }
    }
}
        