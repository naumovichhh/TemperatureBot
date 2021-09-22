using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TemperatureBot.Bot;

namespace TemperatureBot.Controllers
{
    public class BotController : ControllerBase
    {
        private Handler handler;

        public BotController(Handler handler)
        {
            this.handler = handler;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Update update)
        {
            if (update == null)
                return Ok();

            var message = update.Message;
            await handler.Execute(message);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await handler.GetWebhookAsync());
        }
    }
}
