﻿using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class StartCommand : ICommand
    {
        private Notificator notificator;

        public StartCommand(Notificator notificator)
        {
            this.notificator = notificator;
        }

        public string Name => "/start";

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
                notificator.Subscribe(chatId);
                await botClient.SendTextMessageAsync(chatId, "Вы подписались на оповещения.");
            }
            catch (System.Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
