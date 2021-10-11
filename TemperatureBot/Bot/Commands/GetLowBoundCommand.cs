using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class GetLowBoundCommand : ICommand
    {
        private ThermometerObserver notificator;
        private string token;

        public GetLowBoundCommand(ThermometerObserver notificator, string token)
        {
            this.notificator = notificator;
            this.token = token;
        }

        public string Name => "/getlow";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            string token = message.Text.Split(' ')[1];
            if (token != this.token)
            {
                await botClient.SendTextMessageAsync(chatId, "Неправильное значение токена");
                return;
            }

            var lowerBound = notificator.GetLowerBound();
            await botClient.SendTextMessageAsync(chatId, $"Нижний допустимый порог: {lowerBound}.");
        }
    }
}
