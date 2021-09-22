using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class SubscribeCommand : ICommand
    {
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
            long chatId = message.Chat.Id;
            await botClient.SendTextMessageAsync(chatId, $"Инфа - {new Random().Next(101)}%");
        }
    }
}
