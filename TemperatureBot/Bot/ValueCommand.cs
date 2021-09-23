using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class ValueCommand : ICommand
    {
        private ThermometerObserver notificator;

        public ValueCommand(ThermometerObserver notificator)
        {
            this.notificator = notificator;
        }

        public string Name => "/value";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            decimal? temperatureN = notificator.GetCurrentTemperature();
            long chatId = message.Chat.Id;
            if (temperatureN.HasValue)
            {
                await botClient.SendTextMessageAsync(chatId, $"Текущая температура - {temperatureN.Value}°.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
