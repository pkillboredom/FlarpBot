using Discord.Commands;
using Discord.WebSocket;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.GarrysModule
{
    public class Gmod : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly GmodUtil gmodUtil;
        private const long NickUID = 147094239623249920;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Gmod(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            gmodUtil = new GmodUtil(serviceProvider);
        }

        [Command("combatlog")]
        [Summary("Retrieve the last 10 lines of the TTT combat log.")]
        public async Task CombatLog()
        {
            try
            {
                var user = (Context.User as SocketGuildUser);
                logger.Info($"combatlog called by {user}.");
                var isUserNick = user.Id == NickUID;
                if (isUserNick)
                {
                    await ReplyAsync("Lol nah fam get good.");
                }
                else
                {
                    var lastTen = gmodUtil.GetCombatLogLastTen();
                    await ReplyAsync($"Last ten lines of the combat log:{Environment.NewLine}" +
                        $"```{Environment.NewLine}{lastTen}{Environment.NewLine}```");
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                await ReplyAsync("There was an exception running this command. Please check the logs.");
            }
        }
    }
}
