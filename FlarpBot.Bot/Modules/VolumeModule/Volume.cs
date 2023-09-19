using Discord.Commands;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.VolumeModule
{
    public class Volume : ModuleBase<SocketCommandContext>
    {

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly VolumeAnalyzer _volumeAnalyzer;

        public Volume(VolumeAnalyzer volumeAnalyzer)
        {
            _volumeAnalyzer = volumeAnalyzer;
        }

        [Command("volume")]
        [Summary("Analyzes the volume of attached and linked video files.")]
        public async Task VolumeCommand()
        {
            try
            {
                _logger.Info("Volume command called.");
                // check if this message is a reply to another message
                if (Context.Message.ReferencedMessage != null)
                {
                    await _volumeAnalyzer.AnalyzeFromMessage(Context.Message.ReferencedMessage, true, Context.Guild.Id);
                    return;
                }
                else
                {
                    await ReplyAsync("Please reply to a message with this command.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error running VolumeCommand.");
                await ReplyAsync("There was an exception running this command. Please check the logs.");
            }
        }
    }
}
