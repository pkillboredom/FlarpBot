using FlarpBot.Bot.Models;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading.Tasks;

namespace FlarpBot.Bot
{
    public class ExternalRequestHandler
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ExternalRequestHandler(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<ExternalRequestHandlerResponse> SayInChannel(SayInChannelRequest request)
        {
            var client = serviceProvider.GetRequiredService<Discord.WebSocket.DiscordSocketClient>();

            var guild = client.GetGuild(request.GuildId);
            if (guild == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = RequestStatus.Error,
                    RequestMessage = "The guild/server specified did not exist or was unauthorized."
                };
            }

            var channel = guild.GetTextChannel(request.ChannelId);
            if (channel == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = RequestStatus.Error,
                    RequestMessage = "The channel specified did not exist or was unauthorized."
                };
            }

            var message = await channel.SendMessageAsync(request.Message);
            if (message == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = RequestStatus.Error,
                    RequestMessage = "There was an error retrieving the sent message. It may have failed."
                };
            }
            else
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = RequestStatus.Success,
                    RequestMessage = $"Success: {message.Id}"
                };
            }
        }
    }
}
