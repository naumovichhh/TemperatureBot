using System;
using System.Collections.Generic;
using System.Linq;
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
            string url = Configuration.GetSection("BotConfig").GetValue<string>("Url");
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
