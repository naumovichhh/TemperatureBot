using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class StopCommand : ICommand
    {
        private ThermometerObserver notificator;

        public StopCommand(ThermometerObserver notificator)
        {
            this.notificator = notificator;
        }

        public string Name => "/stop";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            try
            {
                if (!notificator.Unsubscribe(chatId))
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы не подписаны.");
                    return;
                }

                await botClient.SendTextMessageAsync(chatId, "Вы отменили подписку.");
            }
            catch (System.Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
