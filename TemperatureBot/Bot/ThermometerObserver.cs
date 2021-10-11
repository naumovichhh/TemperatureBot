using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
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
        private readonly string idsFileName = "Ids";
        private readonly string boundsFileName = "Bounds";

        public ThermometerObserver(IConfiguration configuration)
        {
            Configuration = configuration;
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            /*var webProxy = new WebProxy("10.195.30.50", Port: 8080);
            var httpClient = new HttpClient(
                new HttpClientHandler { Proxy = webProxy, UseProxy = true }
            );
            botClient = new TelegramBotClient(token, httpClient);*/
            botClient = new TelegramBotClient(token);
            ThermometerUri = GetThermometerUri();
            LoadFiles();
        }

        public IConfiguration Configuration { get; set; }
        
        public string ThermometerUri { get; private set; }

        public int UpperBound
        {
            get
            {
                return upperBound;
            }
            set
            {
                if (value <= lowerBound)
                {
                    throw new ArgumentException(nameof(value));
                }

                lock (boundLock)
                {
                    throw new NotImplementedException();
                    upperBound = value;
                }
            }
        }

        public int LowerBound
        {
            get
            {
                return lowerBound;
            }
            set
            {
                if (value >= upperBound)
                {
                    throw new ArgumentException(nameof(value));
                }

                lock (boundLock)
                {
                    throw new NotImplementedException();
                    lowerBound = value;
                }
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

        private void LoadFiles()
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

            if (File.Exists(boundsFileName))
            {
                using (var reader = File.OpenText(boundsFileName))
                {
                    int lowerBound, upperBound;
                    string str;
                    if ((str = reader.ReadLine()) != null)
                    {
                        if (int.TryParse(str, out lowerBound))
                        {
                            this.lowerBound = lowerBound;
                        }
                    }
                    
                    if ((str = reader.ReadLine()) != null)
                    {
                        if (int.TryParse(str, out upperBound))
                        {
                            this.upperBound = upperBound;
                        }
                    }
                }
            }
            else
            {
                File.Create(boundsFileName);
            }
        }

        private void LoadBoundsDefault()
        {
            using (var writer = File.CreateText(boundsFileName))
            {

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
