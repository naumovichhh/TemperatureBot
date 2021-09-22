using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot
{
    public class StartCommand : ICommand
    {
        public string Name => "/start";

        public bool Contained(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
            {
                return false;
            }

            return message.Text.Contains(Name);
        }

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            await botClient.SendTextMessageAsync(chatId, "Привет, это Навальный!");
        }
    }
}
