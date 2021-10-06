using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TemperatureBot.Bot;

namespace TemperatureBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureLogging((ctx, logging) => 
                    {
                        logging.AddEventLog(options => options.SourceName = "Temperature Bot");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddScoped<Handler>();
                    services.AddHostedService<WebhookConfig>();
                    services.AddSingleton<ThermometerObserver>();
                    services.AddHostedService<ThermometerObserver>(p => p.GetRequiredService<ThermometerObserver>());
                });
    }
}
