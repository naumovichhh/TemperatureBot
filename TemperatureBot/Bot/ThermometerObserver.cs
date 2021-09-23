using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace TemperatureBot.Bot
{
    public class ThermometerObserver : BackgroundService
    {
        private List<long> chatIds = new List<long>();
        private object chatIdsLock = new object();
        private object boundLock = new object();
        private int upperBound = 20;
        private int lowerBound = 18;
        private bool tempAcceptable = true;
        private bool initialMeasurement = true;
        private decimal temperature;
        private TelegramBotClient botClient;
        private string idsFileName = "Ids";

        public ThermometerObserver(IConfiguration configuration)
        {
            Configuration = configuration;
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            botClient = new TelegramBotClient(token);
            ThermometerUri = GetThermometerUri();
            LoadFile();
            //Instance = this;
        }

        //public static Notificator Instance { get; private set; }
        public IConfiguration Configuration { get; set; }
        public string ThermometerUri { get; private set; }

        public void SetUpperBound(int value)
        {
            if (value <= lowerBound)
            {
                throw new ArgumentException(nameof(value));
            }

            lock (boundLock)
            {
                upperBound = value;
            }
        }

        public void SetLowerBound(int value)
        {
            if (value >= upperBound)
            {
                throw new ArgumentException(nameof(value));
            }

            lock (boundLock)
            {
                lowerBound = value;
            }
        }

        public bool Subscribe(long chatId)
        {
            lock (chatIdsLock)
            {
                if (chatIds.Contains(chatId))
                {
                    return false;
                }
                else
                {
                    chatIds.Add(chatId);
                    RefreshFile();
                    return true;
                }
            }
        }

        public bool Unsubscribe(long chatId)
        {
            lock (chatIdsLock)
            {
                if (!chatIds.Remove(chatId))
                {
                    return false;
                }
                else
                {
                    RefreshFile();
                    return true;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            while (!stoppingToken.IsCancellationRequested)
            {
                var read = GetCurrentTemperature();
                if (read.HasValue)
                {
                    HandleNotifications(read.Value);
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }

        private void RefreshFile()
        {
            using (var writer = new StreamWriter(File.Open(idsFileName, FileMode.Create)))
            {
                foreach (var chatId in chatIds)
                {
                    writer.WriteLine(chatId);
                }
            }
        }

        private void LoadFile()
        {
            if (File.Exists(idsFileName))
            {
                using (var reader = File.OpenText(idsFileName))
                {
                    long chatId;
                    string str;
                    while ((str = reader.ReadLine()) != null)
                    {
                        if (long.TryParse(str, out chatId))
                        {
                            chatIds.Add(chatId);
                        }
                    }
                }
            }
            else
            {
                File.Create(idsFileName).Close();
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
                    Notify(string.Format("Температура выше верхнего допустимого порога: {0}°.", measured));
                }
            }

            if (Math.Ceiling(measured) > Math.Ceiling(temperature))
            {
                if (IsTempAcceptable(measured) && !tempAcceptable)
                {
                    Notify(string.Format("Температура вернулась к допустимому значению: {0}°.", measured));
                }
            }

            if (Math.Ceiling(measured) < Math.Ceiling(temperature))
            {
                if (measured <= lowerBound)
                {
                    Notify(string.Format("Температура ниже нижнего допустимого порога: {0}°.", measured));
                }
            }

            if (Math.Floor(measured) < Math.Floor(temperature))
            {
                if (IsTempAcceptable(measured) && !tempAcceptable)
                {
                    Notify(string.Format("Температура вернулась к допустимому значению: {0}°.", measured));
                }
            }

            SetIfTempAcceptable(measured);
            temperature = measured;
        }

        private void SetIfTempAcceptable(decimal measured)
        {
            if (IsTempAcceptable(measured))
            {
                tempAcceptable = true;
            }
            else
                tempAcceptable = false;
        }

        private bool IsTempAcceptable(decimal measured)
        {
            return measured > lowerBound && measured < upperBound;
        }

        private void Notify(string text)
        {
            long[] array;
            lock (chatIdsLock)
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
