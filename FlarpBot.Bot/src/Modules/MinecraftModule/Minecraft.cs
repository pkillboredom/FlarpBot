using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Discord_Bot.Modules.MinecraftModule
{
    public class Minecraft : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly MinecraftUtil mcUtil;

        public Minecraft(IServiceProvider _serviceProvider)
        {
            serviceProvider = _serviceProvider;
            mcUtil = new MinecraftUtil(serviceProvider);
        }

        [Command("mcstatus")] // Command name.
        [Summary("Fetch the status of the Minecraft Server")] // Command summary.
        public async Task MCStatus()
        {
            try
            {
                var status = mcUtil.GetServerStatus();
                if (status.ServerUp)
                    // Only show port if not default.
                    await ReplyAsync($"The Minecraft server at **{(status.Host + (status.Port != 25565 ? status.Port : ""))}** is **UP**!\n" +
                        $"There are **{status.CurrentPlayers} players** connected out of a maximum of {status.MaximumPlayers}.\n" +
                        $"The server says: \"{status.Motd}\".");
                else
                    await ReplyAsync($"The Minecraft Server at **{(status.Host + (status.Port != 25565 ? status.Port : ""))}** is **DOWN**.");

            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}
