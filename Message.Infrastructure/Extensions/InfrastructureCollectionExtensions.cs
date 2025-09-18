using MassTransit;
using Message.Infrastructure.Interfaces;
using Message.Infrastructure.Services;
using Message.Infrastructure.Services.Consumers;
using Message.Infrastructure.Services.Senders;
using Message.Infrastructure.Services.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Message.Repository;

namespace Message.Infrastructure.Extensions;

public static class InfrastructureCollectionExtensions
{
    public static void AddMessagingConfiguration(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationRepository>(options => 
            options.UseSqlServer(defaultConnectionString));

        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ISenderService, EmailSenderService>();
        
        services.AddHostedService<OutboxPublisherWorker>();
        
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<MessageSendConsumer>(cfg => { cfg.ConcurrentMessageLimit = 32; });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(
                    builder.Configuration["Rabbit:Host"] ?? "localhost",
                    builder.Configuration.GetValue<ushort>("Rabbit:Port", 5672),
                    builder.Configuration["Rabbit:VHost"] ?? "/",
                    h =>
                    {
                        h.Username(builder.Configuration["Rabbit:User"] ?? "guest");
                        h.Password(builder.Configuration["Rabbit:Pass"] ?? "guest");
                    });


                cfg.UseDelayedMessageScheduler();

                cfg.ReceiveEndpoint("message-send-queue", e =>
                {
                    e.PrefetchCount = 64;

                    e.UseMessageRetry(r =>
                        r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

                    e.ConfigureConsumeTopology = true;

                    e.ConfigureConsumer<MessageSendConsumer>(ctx);
                });
                
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }
}