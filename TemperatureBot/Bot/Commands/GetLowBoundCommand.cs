using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class GetLowBoundCommand : ICommand
    {
        private Thermometer thermometer;
        private string token;

        public GetLowBoundCommand(Thermometer thermometer, string token)
        {
            this.thermometer = thermometer;
            this.token = token;
        }

        public string Name => "/getlow";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            try
            {
                var lowerBound = thermometer.LowerBound;
                await botClient.SendTextMessageAsync(chatId, $"Нижний допустимый порог: {lowerBound}.");
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
            }
        }
    }
}
