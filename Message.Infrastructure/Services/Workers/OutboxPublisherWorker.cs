using MassTransit;
using Message.Domain.Records;
using Message.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Message.Infrastructure.Services.Workers;

public class OutboxPublisherWorker(
    ILogger<OutboxPublisherWorker> logger,
    IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationRepository>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                
                var now = DateTime.UtcNow;

                var messages = await db.OutboxMessages
                    .Where(x => x.ProcessedOn == null)
                    .OrderBy(x => x.OccurredOn)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        if (msg.Type == nameof(MessageSendRequested))
                        {
                            var evt = JsonConvert.DeserializeObject<MessageSendRequested>(msg.Payload)!;
                            
                            if (msg.ScheduledAt is not null && msg.ScheduledAt > now)
                            {
                                var delay = msg.ScheduledAt.Value - now;
                                await publishEndpoint.Publish(evt, ctx => ctx.Delay = delay, stoppingToken);
                            }
                            else
                            {
                                await publishEndpoint.Publish(evt, stoppingToken);
                            }
                        }

                        msg.ProcessedOn = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.Error = ex.Message;
                        logger.LogError(ex, "Failed to publish OutboxMessage {Id}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxPublisherWorker fatal error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}