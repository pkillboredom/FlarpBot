using Microsoft.AspNetCore.Mvc;
using FlarpBot.Bot.Models;
using FlarpBot.Bot;
using System.Threading.Tasks;

namespace FlarpBot.WebApi.Controllers
{
    public class SayController : Controller
    {
        private readonly DiscordBot discordBot;

        public SayController(DiscordBot _discordBot)
        {
            discordBot = _discordBot;
        }

        [HttpPost]
        public async Task<IActionResult> SayInChannel(SayInChannelRequest request)
        {
            if (request == null)
            {
                return new BadRequestResult();
            }

            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                return new BadRequestObjectResult("RequestId cannot be blank");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new BadRequestObjectResult("Message cannot be blank");
            }

            var externalRequestHandler = discordBot.GetExternalRequestHandler();
            var sayInChannelResponse = await externalRequestHandler.SayInChannel(request);

            if (sayInChannelResponse.RequestStatus == RequestStatus.Error)
            {
                return new BadRequestObjectResult(sayInChannelResponse.RequestMessage);
            }
            else
            {
                return new OkObjectResult(sayInChannelResponse.RequestMessage);
            }
        }
    }
}
