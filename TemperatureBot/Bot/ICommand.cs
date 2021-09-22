using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    interface ICommand
    {
        string Name { get; }
        Task Execute(Message message, TelegramBotClient botClient);
        bool Contained(Message message);
    }
}
