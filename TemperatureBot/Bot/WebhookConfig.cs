using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace TemperatureBot.Bot
{
    public class WebhookConfig : IHostedService
    {
        public WebhookConfig(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            string url = Configuration.GetSection("BotConfig").GetValue<string>("Webhook");
            /*var webProxy = new WebProxy("10.195.30.50", Port: 8080);
            var httpClient = new HttpClient(
                new HttpClientHandler { Proxy = webProxy, UseProxy = true }
            );
            var botClient = new TelegramBotClient(token, httpClient);*/
            var botClient = new TelegramBotClient(token);
            string hook = url + "/bot/update/" + token;
            return botClient.SetWebhookAsync(hook);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
