using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class GetHighBoundCommand : ICommand
    {
        private Thermometer thermometer;
        private string token;

        public GetHighBoundCommand(Thermometer thermometer, string token)
        {
            this.thermometer = thermometer;
            this.token = token;
        }

        public string Name => "/gethigh";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            try
            {
                var lowerBound = thermometer.UpperBound;
                await botClient.SendTextMessageAsync(chatId, $"Верхний допустимый порог: {lowerBound}.");
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
