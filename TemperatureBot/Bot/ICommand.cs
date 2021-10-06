using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    interface ICommand
    {
        string Name { get; }
        Task Execute(Message message, TelegramBotClient botClient);
        bool Contained(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
            {
                return false;
            }

            return message.Text.Split(' ', '@')[0] == Name;
        }
    }
}
