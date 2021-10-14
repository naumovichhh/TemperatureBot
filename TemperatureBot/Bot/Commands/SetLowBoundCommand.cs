using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TemperatureBot.Bot.Commands
{
    public class SetLowBoundCommand : ICommand
    {
        private Thermometer thermometer;
        private string token;

        public SetLowBoundCommand(Thermometer thermometer, string token)
        {
            this.thermometer = thermometer;
            this.token = token;
        }

        public string Name => "/setlow";

        public async Task Execute(Message message, TelegramBotClient botClient)
        {
            long chatId = message.Chat.Id;
            try
            {
                string token = message.Text.Split(' ')[1];
                if (token != this.token)
                {
                    await botClient.SendTextMessageAsync(chatId, "Неправильное значение токена.");
                    return;
                }

                int value = int.Parse(message.Text.Split(' ')[2]);
                thermometer.LowerBound = value;
                await botClient.SendTextMessageAsync(chatId, $"Нижний допустимый порог установлен: {value}.");
            }
            catch (System.Exception)
            {
                try
                {
                    await botClient.SendTextMessageAsync(chatId, "Произошла ошибка.");
                }
                catch (System.Exception)
                {
                }
            }
        }
    }
}
