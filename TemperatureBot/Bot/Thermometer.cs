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
    public class Thermometer : BackgroundService
    {
        private List<long> chatIds = new List<long>();
        private object chatIdsLock = new object();
        private object boundsLock = new object();
        private int upperBound;
        private int lowerBound;
        private bool tempAcceptable = true;
        private bool initialMeasurement = true;
        private decimal temperature;
        private TelegramBotClient botClient;
        private readonly string idsFileName = "Ids";
        private readonly string boundsFileName = "Bounds";

        public Thermometer(IConfiguration configuration)
        {
            Configuration = configuration;
            string token = Configuration.GetSection("BotConfig").GetValue<string>("Token");
            var webProxy = new WebProxy("10.195.30.50", Port: 8080);
            var httpClient = new HttpClient(
                new HttpClientHandler { Proxy = webProxy, UseProxy = true }
            );
            botClient = new TelegramBotClient(token, httpClient);
            //botClient = new TelegramBotClient(token);
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

                lock (boundsLock)
                {
                    upperBound = value;
                }

                RefreshBoundsFile();
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

                lock (boundsLock)
                {
                    lowerBound = value;
                }

                RefreshBoundsFile();
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
                    RefreshChatIdsFile();
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
                    RefreshChatIdsFile();
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

        private void RefreshChatIdsFile()
        {
            List<long> chatIds;
            lock (chatIdsLock)
            {
                chatIds = this.chatIds;
            }
            
            using (var writer = new StreamWriter(File.Open(idsFileName, FileMode.Create)))
            {
                foreach (var chatId in chatIds)
                {
                    writer.WriteLine(chatId);
                }
            }
        }

        private void RefreshBoundsFile()
        {
            int lowerBound, upperBound;
            lock (boundsLock)
            {
                lowerBound = this.lowerBound;
                upperBound = this.upperBound;
            }

            using (var writer = File.CreateText(boundsFileName))
            {
                writer.WriteLine(lowerBound);
                writer.WriteLine(upperBound);
            }
        }

        private void LoadFiles()
        {
            LoadChatIdsFile();
            LoadBoundsFile();
        }

        private void LoadChatIdsFile()
        {
            List<long> chatIds = new List<long>();

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

            lock (chatIdsLock)
            {
                this.chatIds = chatIds;
            }
        }

        private void LoadBoundsFile()
        {
            if (File.Exists(boundsFileName))
            {
                using (var reader = File.OpenText(boundsFileName))
                {
                    int lowerBound, upperBound;
                    string str;
                    if ((str = reader.ReadLine()) != null && int.TryParse(str, out lowerBound))
                    {
                        this.lowerBound = lowerBound;
                    }
                    else
                    {
                        LoadBoundsFileDefault();
                    }

                    if ((str = reader.ReadLine()) != null && int.TryParse(str, out upperBound))
                    {
                        this.upperBound = upperBound;
                    }
                    else
                    {
                        LoadBoundsFileDefault();
                    }
                }
            }
            else
            {
                LoadBoundsFileDefault();
            }
        }

        private void LoadBoundsFileDefault()
        {
            lowerBound = 18;
            upperBound = 20;
            RefreshBoundsFile();
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
