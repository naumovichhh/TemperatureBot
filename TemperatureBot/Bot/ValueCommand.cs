using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class ValueCommand : ICommand
    {
        private Notificator notificator;

        public ValueCommand(Notificator notificator)
        {
            this.notificator = notificator;
        }

        public string Name => "/value";

        public bool Contained(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
            {
                return false;
            }

            return message.Text.Split(' ')[0] == Name;
        }

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            decimal? temperatureN = notificator.GetCurrentTemperature();
            long chatId = message.Chat.Id;
            if (temperatureN.HasValue)
            {
                await botClient.SendTextMessageAsync(chatId, $"Текущая температура - {temperatureN.Value} градусов.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
