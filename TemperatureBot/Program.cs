using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
