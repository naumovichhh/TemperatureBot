using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using TemperatureBot.Bot.Commands;

namespace TemperatureBot.Bot
{
    public class Handler
    {
        private TelegramBotClient botClient;
        private List<ICommand> commands = new List<ICommand>();

        public Handler(IConfiguration configuration, Thermometer thermometer)
        {
            Configuration = configuration;
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            commands.Add(new StartCommand(thermometer));
            commands.Add(new StopCommand(thermometer));
            commands.Add(new ValueCommand(thermometer));
            commands.Add(new SetHighBoundCommand(thermometer, token));
            commands.Add(new SetLowBoundCommand(thermometer, token));
            commands.Add(new GetHighBoundCommand(thermometer, token));
            commands.Add(new GetLowBoundCommand(thermometer, token));
            var webProxy = new WebProxy("10.195.30.50", Port: 8080);
            var httpClient = new HttpClient(
                new HttpClientHandler { Proxy = webProxy, UseProxy = true }
            );
            botClient = new TelegramBotClient(token, httpClient);
            //botClient = new TelegramBotClient(token);
        }

        public IConfiguration Configuration { get; set; }

        public async Task<WebhookInfo> GetWebhookAsync()
        {
            return await botClient.GetWebhookInfoAsync();
        }

        public async Task Execute(Message message)
        {
            foreach (var command in commands)
            {
                if (command.Contained(message))
                {
                    await command.Execute(message, botClient);
                }
            }
        }
    }
}
