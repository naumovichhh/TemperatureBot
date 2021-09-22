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
        private IServiceProvider serviceProvider;

        public WebhookConfig(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            Configuration = configuration;
            this.serviceProvider = serviceProvider;
        }

        public IConfiguration Configuration { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            string url = Configuration.GetSection("BotConfig").GetValue<string>("Url");
            var botClient = new TelegramBotClient(token);
            string hook = url + "/api/update/" + token;
            return botClient.SetWebhookAsync(hook);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
