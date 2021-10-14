using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class StartCommand : ICommand
    {
        private Thermometer thermometer;

        public StartCommand(Thermometer thermometer)
        {
            this.thermometer = thermometer;
        }

        public string Name => "/start";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            try
            {
                if (!thermometer.Subscribe(chatId))
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы уже подписаны.");
                    return;
                }

                await botClient.SendTextMessageAsync(chatId, "Вы подписались на оповещения.");
            }
            catch (System.Exception)
            {
                try
                {
                    await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
                }
                catch (System.Exception)
                {
                }
            }
        }
    }
}
