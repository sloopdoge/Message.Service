using Message.Infrastructure.Interfaces;
using Message.Infrastructure.Services;
using Message.Infrastructure.Services.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Message.Repository;

namespace Message.Infrastructure.Extensions;

public static class InfrastructureCollectionExtensions
{
    public static WebApplicationBuilder AddMessagingConfiguration(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationRepository>(options => 
            options.UseSqlServer(defaultConnectionString));

        services.AddScoped<IMessageService, MessageService>();
        services.AddHostedService<MessageSenderWorkerService>();

        services.AddScoped<ISenderService, EmailSenderService>();
        
        return builder;
    }
}