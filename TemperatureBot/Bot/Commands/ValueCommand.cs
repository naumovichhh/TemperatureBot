using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class ValueCommand : ICommand
    {
        private Thermometer thermometer;

        public ValueCommand(Thermometer notificator)
        {
            this.thermometer = notificator;
        }

        public string Name => "/value";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            decimal? temperatureN = thermometer.GetCurrentTemperature();
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
