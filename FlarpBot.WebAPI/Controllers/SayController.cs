using Microsoft.AspNetCore.Mvc;
using FlarpBot.Bot.Models;
using FlarpBot.Bot;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FlarpBot.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SayController : ControllerBase
    {
        private readonly DiscordBot discordBot;

        public SayController(DiscordBot _discordBot)
        {
            discordBot = _discordBot;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SayInChannel(SayInChannelRequest request)
        {
            if (request == null)
            {
                return new BadRequestResult();
            }

            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                return new BadRequestObjectResult(new ExternalRequestHandlerResponse
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "RequestId Missing."
                });
            }
            if (string.IsNullOrWhiteSpace(request.GuildId))
            {
                return new BadRequestObjectResult(new ExternalRequestHandlerResponse
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "GuildId Missing."
                });
            }
            if (string.IsNullOrWhiteSpace(request.ChannelId))
            {
                return new BadRequestObjectResult(new ExternalRequestHandlerResponse
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "ChannelId Missing."
                });
            }
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new BadRequestObjectResult(new ExternalRequestHandlerResponse
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "Message Missing."
                });
            }

            var externalRequestHandler = discordBot.GetExternalRequestHandler();
            var sayInChannelResponse = await externalRequestHandler.SayInChannel(request);

            if (sayInChannelResponse.RequestStatus == "Error")
            {
                return new BadRequestObjectResult(sayInChannelResponse);
            }
            else
            {
                return new OkObjectResult(sayInChannelResponse);
            }
        }
    }
}
