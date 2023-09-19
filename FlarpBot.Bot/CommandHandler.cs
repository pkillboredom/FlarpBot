using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FlarpBot.Bot.Modules.VolumeModule;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FlarpBot.Bot
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly Functions _functions;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly VolumeAnalyzer _volumeAnalyzer;

        public CommandHandlingService(IServiceProvider services)
        {

            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
            _functions = services.GetRequiredService<Functions>();
            _volumeAnalyzer = services.GetRequiredService<VolumeAnalyzer>();

            // Event handlers
            _client.Ready += ClientReadyAsync;
            _client.MessageReceived += HandleCommandAsync;
            _client.JoinedGuild += SendJoinMessageAsync;
        }

        private async Task HandleCommandAsync(SocketMessage rawMessage)
        {
            try
            {
                if (rawMessage.Author.IsBot || !(rawMessage is SocketUserMessage message) || message.Channel is IDMChannel)
                    return;

                var context = new SocketCommandContext(_client, message);

                int argPos = 0;

                var config = _functions.GetConfig();
                string prefix = config["prefix"];

                // Check if message has any of the prefixes or mentiones the bot.
                if (message.HasStringPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    // Execute the command.
                    var result = await _commands.ExecuteAsync(context, argPos, _services);

                    if (!result.IsSuccess && result.Error.HasValue)
                        await context.Channel.SendMessageAsync($":x: {result.ErrorReason}");
                }
                else
                {
                    await _volumeAnalyzer.AnalyzeFromMessage(message, false, context.Guild.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private async Task SendJoinMessageAsync(SocketGuild guild)
        {
            var config = _functions.GetConfig();
            string joinMessage = config["join_message"];

            if (string.IsNullOrEmpty(joinMessage))
                return;

            // Send the join message in the first channel where the bot can send messsages.
            foreach (var channel in guild.TextChannels.OrderBy(x => x.Position))
            {
                var botPerms = channel.GetPermissionOverwrite(_client.CurrentUser).GetValueOrDefault();

                if (botPerms.SendMessages == PermValue.Deny)
                    continue;

                try
                {
                    await channel.SendMessageAsync(joinMessage);
                    return;
                }
                catch
                {
                    continue;
                }
            }
        }

        private async Task ClientReadyAsync()
            => await _functions.SetBotStatusAsync(_client);

        public async Task InitializeAsync()
            => await _commands.AddModulesAsync(Assembly.GetAssembly(typeof(DiscordBot)), _services);
    }
}