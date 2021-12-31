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

            ulong u_guildId = Convert.ToUInt64(request.GuildId);

            var guild = client.GetGuild(u_guildId);
            if (guild == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "The guild/server specified did not exist or was unauthorized."
                };
            }

            //SocketGuild guild;
            //try
            //{
            //    guild = client.Guilds.Single(g => g.Id == request.GuildId);
            //}
            //catch (Exception ex)
            //{
            //    return new ExternalRequestHandlerResponse()
            //    {
            //        RequestId = request.RequestId,
            //        RequestStatus = "Error",
            //        RequestMessage = "The guild/server specified could not be found."
            //    };
            //}

            ulong u_channelId = Convert.ToUInt64(request.ChannelId);

            var channel = guild.GetTextChannel(u_channelId);
            if (channel == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "The channel specified did not exist or was unauthorized."
                };
            }

            var message = await channel.SendMessageAsync(request.Message);
            if (message == null)
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Error",
                    RequestMessage = "There was an error retrieving the sent message. It may have failed."
                };
            }
            else
            {
                return new ExternalRequestHandlerResponse()
                {
                    RequestId = request.RequestId,
                    RequestStatus = "Success",
                    RequestMessage = $"Success: {message.Id}"
                };
            }
        }
    }
}
