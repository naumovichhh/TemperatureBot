using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class Handler
    {
        private TelegramBotClient botClient;
        private List<ICommand> commands = new List<ICommand>();

        public Handler(IConfiguration configuration, ThermometerObserver notificator)
        {
            Configuration = configuration;
            commands.Add(new StartCommand(notificator));
            commands.Add(new StopCommand(notificator));
            commands.Add(new ValueCommand(notificator));
            commands.Add(new SetHighBoundCommand(notificator, Configuration.GetSection("BotConfig").GetValue<string>("Token")));
            commands.Add(new SetLowBoundCommand(notificator, Configuration.GetSection("BotConfig").GetValue<string>("Token")));
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            botClient = new TelegramBotClient(token);
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
