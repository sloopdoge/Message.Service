using Message.Infrastructure.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace Message.Application;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        #region Serilog Logger

        if (builder.Environment.IsDevelopment())
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();
        }

        if (builder.Environment.IsProduction())
        {
            var lokiUri = builder.Configuration.GetValue<string>("LokiSettings:Url");
            var appName = builder.Configuration.GetValue<string>("LokiSettings:AppName");
            var serviceName = builder.Configuration.GetValue<string>("LokiSettings:ServiceName");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.GrafanaLoki(
                    lokiUri!,
                    labels:
                    [
                        new LokiLabel
                        {
                            Key = "app",
                            Value = appName!
                        },
                        new LokiLabel
                        {
                            Key = "service",
                            Value = serviceName!
                        }
                    ],
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();
        }

        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();

        #endregion
        
        Log.Warning("Starting web host");
        try
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.AddMessagingConfiguration();
            
            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            
            app.Run();
        }
        catch (Exception e)
        {
            Log.Fatal(e, e.Message);
        }
        finally
        {
            Log.Warning("Web host shutdown");
            Log.CloseAndFlush();
        }
    }
}