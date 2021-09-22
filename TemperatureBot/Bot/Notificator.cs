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

namespace TemperatureBot.Bot
{
    public class Notificator : BackgroundService
    {
        private List<long> chatIds = new List<long>();
        private object lockObj = new object();
        private IConfiguration configuration;
        public Notificator(IConfiguration configuration)
        {
            this.configuration = configuration;
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

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private decimal? GetCurrentTemperature(string uri)
        {
            using (var reader = XmlReader.Create(uri))
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
