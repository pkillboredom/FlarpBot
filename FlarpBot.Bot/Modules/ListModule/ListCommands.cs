using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.MediaModule
{

    public class ListCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private readonly ExternalRequestHandler externalRequestHandler;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, string[]> CommandListDict;
        public ListCommands(IServiceProvider serviceProvider)
        {
            try
            {
                this.serviceProvider = serviceProvider;
                configuration = this.serviceProvider.GetRequiredService<IConfiguration>();
                externalRequestHandler = serviceProvider.GetRequiredService<ExternalRequestHandler>();

                LoadImageURLLists();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            foreach (var kv in CommandListDict)
            {
                Random r = new Random();
                builder = builder.AddCommand(kv.Key, async (context, unk, servProvider, commandInfo) => {
                    var randomItem = kv.Value[r.Next(kv.Value.Length)];
                    await externalRequestHandler.SayInChannel(new Models.SayInChannelRequest
                    {
                        GuildId = context.Guild.Id.ToString(),
                        ChannelId = context.Channel.Id.ToString(),
                        RequestId = new Guid().ToString(),
                        Message = randomItem
                    });
                    await context.Message.DeleteAsync();
                }, commandBuilder => { });
            }
            base.OnModuleBuilding(commandService, builder);
        }

        private void LoadImageURLLists()
        {
            CommandListDict = new Dictionary<string, string[]>();
            var CommandDict = configuration.GetSection("ListCommands").Get<Dictionary<string, string>>();
            foreach (var commandListPathPair in CommandDict)
            {
                var urlList = File.ReadAllLines(commandListPathPair.Value);
                _ = CommandListDict.TryAdd(commandListPathPair.Key, urlList);
            }
        }
    }
}
