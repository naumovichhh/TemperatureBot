using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class StopCommand : ICommand
    {
        private Notificator notificator;

        public StopCommand(Notificator notificator)
        {
            this.notificator = notificator;
        }

        public string Name => "/stop";

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
            long chatId = message.Chat.Id;
            try
            {
                notificator.Unsubscribe(chatId);
                await botClient.SendTextMessageAsync(chatId, "Вы отменили подписку.");
            }
            catch (System.Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
