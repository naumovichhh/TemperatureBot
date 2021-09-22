using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace TemperatureBot.Bot
{
    public class Notificator : BackgroundService
    {
        private List<long> chatIds = new List<long>();
        private object lockObj = new object();
        private int upperBound = 21;
        private int lowerBound = 17;
        private bool initialMeasurement = true;
        private decimal temperature;
        private TelegramBotClient botClient;

        public Notificator(IConfiguration configuration)
        {
            Configuration = configuration;
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            botClient = new TelegramBotClient(token);
            ThermometerUri = GetThermometerUri();
            //Instance = this;
        }

        //public static Notificator Instance { get; private set; }
        public IConfiguration Configuration { get; set; }
        public string ThermometerUri { get; private set; }

        public void SetUpperBound(int value)
        {
            upperBound = value;
        }

        public void SetLowerBound(int value)
        {
            lowerBound = value;
        }

        public void Subscribe(long chatId)
        {
            lock (lockObj)
            {
                chatIds.Add(chatId);
            }
        }

        public void Unsubscribe(long chatId)
        {
            lock (lockObj)
            {
                chatIds.Remove(chatId);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            while (!stoppingToken.IsCancellationRequested)
            {
                decimal temperature;
                var read = GetCurrentTemperature();
                if (read.HasValue)
                {
                    HandleNotifications(read.Value);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private void HandleNotifications(decimal measured)
        {
            if (initialMeasurement)
            {
                temperature = measured;
                initialMeasurement = false;
                return;
            }

            if (Math.Floor(measured) > Math.Floor(temperature))
            {
                if (measured >= upperBound)
                {
                    Notify(string.Format("Температура выше верхнего допустимого порога: {0} градусов.", measured));
                }
                else if (measured >= lowerBound)
                {
                    Notify(string.Format("Температура вернулась к допустимому значению."));
                }
            }

            if (Math.Ceiling(measured) < Math.Ceiling(temperature))
            {
                if (measured <= lowerBound)
                {
                    Notify(string.Format("Температура ниже нижнего допустимого порога: {0} градусов.", measured));
                }
                else if (measured <= upperBound)
                {
                    Notify(string.Format("Температура вернулась к допустимому значению."));
                }
            }

            temperature = measured;
        }

        private void Notify(string text)
        {
            long[] array;
            lock (lockObj)
            {
                array = chatIds.ToArray();
            }

            Parallel.ForEach(array, chatId => botClient.SendTextMessageAsync(chatId, text));
        }

        private string GetThermometerUri()
        {
            string uri = Configuration.GetSection("BotConfig").GetValue<string>("Thermometer");
            return uri + "/val.xml";
        }

        public decimal? GetCurrentTemperature()
        {
            using (var reader = XmlReader.Create(ThermometerUri))
            {
                while (reader.Read())
                {
                    if (reader.Name == "term0" && reader.IsStartElement() && reader.Read())
                    {
                        decimal measured;
                        if (decimal.TryParse(reader.Value, out measured) || decimal.TryParse(reader.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out measured))
                        {
                            return measured;
                        }
                    }
                }
            }

            return null;
        }
    }
}
