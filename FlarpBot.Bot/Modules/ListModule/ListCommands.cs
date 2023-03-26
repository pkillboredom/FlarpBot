using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.MediaModule
{

    public class VoiceListCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private readonly ExternalRequestHandler externalRequestHandler;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, string[]> CommandListDict;
        private Dictionary<string, string[]> VoiceListDict;
        private static readonly string[] audioExtensions = new string[] { ".3ga", ".aac", ".ac3", ".aif", ".aiff", ".amr", ".au", ".caf", ".dts", ".flac", ".m4a", ".m4b", ".m4p", ".mka", ".mp3", ".mpc", ".oga", ".ogg", ".opus", ".ra", ".ram", ".spx", ".tta", ".wav", ".wma" };

        private readonly SemaphoreSlim voiceSem = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<ulong, DateTime> voiceCooldownExpirys = new ConcurrentDictionary<ulong, DateTime>();

        private const ulong lukeUid = 135270689815920641;

        public VoiceListCommands(IServiceProvider serviceProvider)
        {
            try
            {
                this.serviceProvider = serviceProvider;
                configuration = this.serviceProvider.GetRequiredService<IConfiguration>();
                externalRequestHandler = serviceProvider.GetRequiredService<ExternalRequestHandler>();

                LoadImageURLLists();
                LoadVoicePathLists();
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
                    //await context.Message.DeleteAsync();
                }, commandBuilder => { });
            }
            foreach (var kv in VoiceListDict)
            {
                Random r = new Random();
                builder = builder.AddCommand(kv.Key, async (context, unk, servProvider, commandInfo) => {
                    // If we cannot acquire immediately, someone else is using a voice command, just ignore command.
                    if (voiceSem.Wait(0))
                    {
                        try
                        {
                            if (context.User.Id != lukeUid)
                            {
                                // If this user is in the cooldown dict, and the cooldown has not expired, ignore command.
                                if (voiceCooldownExpirys.TryGetValue(context.User.Id, out DateTime expiry) && expiry > DateTime.Now)
                                {
                                    await context.Channel.SendMessageAsync("You must wait 60 seconds between voice commands.");
                                    return;
                                }
                                else
                                {
                                    voiceCooldownExpirys.AddOrUpdate(context.User.Id, DateTime.Now.AddSeconds(60), (k, v) => DateTime.Now.AddSeconds(60));
                                }
                            }
                            var randomItem = kv.Value[r.Next(kv.Value.Length)];
                            IVoiceChannel channel = (context.User as IGuildUser)?.VoiceChannel;
                            if (channel == null)
                            {
                                await context.Channel.SendMessageAsync("You must be in a voice channel to use this command.");
                                return;
                            }
                            else
                            {
                                var audioClient = await channel.ConnectAsync();
                                await SendAsync(audioClient, randomItem);
                                audioClient.Dispose();
                                return;
                            }
                        }
                        finally
                        {
                            voiceSem.Release();
                        }
                    }
                    else
                    {
                        return;
                    }
                    
                }, commandBuilder => { commandBuilder.RunMode = RunMode.Async; });
            }
            base.OnModuleBuilding(commandService, builder);
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            try
            {
                using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
                using (var ffmpeg = CreateStream(path))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                {
                    try { await output.CopyToAsync(discord); }
                    catch (Exception ex) { 
                        logger.Error(ex);
                    }
                    finally { 
                        await discord.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private void LoadImageURLLists()
        {
            CommandListDict = new Dictionary<string, string[]>();
            var CommandDict = configuration.GetSection("ListCommands").Get<Dictionary<string, string>>();
            foreach (var commandListPathPair in CommandDict)
            {
                var urlList = File.ReadAllLines(commandListPathPair.Value);
                CommandListDict.Add(commandListPathPair.Key, urlList);
            }
        }

        private void LoadVoicePathLists()
        {
            VoiceListDict = new Dictionary<string, string[]>();
            var CommandDict = configuration.GetSection("VoiceCommands").Get<Dictionary<string, string>>();
            foreach (var commandListPathPair in CommandDict)
            {
                var pathList = File.ReadAllLines(commandListPathPair.Value);
                VoiceListDict.Add(commandListPathPair.Key, pathList);
            }
            var VoiceDirList = configuration.GetSection("VoiceDirs").Get<List<string>>();
            foreach (var directory in VoiceDirList)
            {
                string[] audioFilePaths = GetAudioFilePaths(directory);
                foreach (string audioFilePath in audioFilePaths)
                {
                    string commandName = Path.GetFileNameWithoutExtension(audioFilePath);
                    if (!VoiceListDict.ContainsKey(commandName))
                    {
                        VoiceListDict.Add(commandName, new string[] { audioFilePath });
                    }
                    else
                    {
                        int i = 1;
                        while (VoiceListDict.ContainsKey($"{commandName}-{i}"))
                        {
                            if (i > 10)
                            {
                                logger.Error($"There are too many commands with the name {commandName}, please re-check your configs.");
                                throw new Exception($"There are too many commands with the name {commandName}, please re-check your configs.");
                            }
                            i++;
                        }
                        VoiceListDict.Add(commandName, new string[] { audioFilePath });
                    }
                }

            }
        }
        private static string[] GetAudioFilePaths(string directoryPath)
        {
            // Get all files in the directory with any of the audio extensions
            string[] audioFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                .Where(file => audioExtensions.Contains(Path.GetExtension(file)))
                .ToArray();

            return audioFiles;
        }
    }
}
