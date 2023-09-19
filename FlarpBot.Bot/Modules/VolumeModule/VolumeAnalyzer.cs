using Discord;
using EasyCaching.Core;
using FlarpBot.Bot.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.VolumeModule
{
    public class VolumeAnalyzer
    {
        private readonly string[] videoExtensions;
        private const double DefaultMaxLoudness = -8.0;
        private const uint DefaultMaxMessageSize = 50 * 1024 * 1024; // 50MiB in Bytes. 
        private static StringIgnoreCaseEqualityComparer comparer = new StringIgnoreCaseEqualityComparer();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly HttpClient _client;
        private readonly IConfiguration _config;
        private readonly IEasyCachingProviderFactory _cacheFactory;

        public VolumeAnalyzer(HttpClient client, IConfiguration configuration, IEasyCachingProviderFactory easyCachingProviderFactory)
        {
            _client = client;
            _config = configuration;
            _cacheFactory = easyCachingProviderFactory;

            videoExtensions = _config.GetSection("VolumeAnalyzer:VideoExtensions").Get<string[]>();
        }

        public async Task AnalyzeFromMessage(IUserMessage message, bool isUserInitiated, ulong guildId, uint maxMessageSize = DefaultMaxMessageSize)
        {
            // Check if message has video attachments or embeds.
            var attachments = message.Attachments.Where(a => videoExtensions.Contains(Path.GetExtension(a.Url), comparer));
            var embeds = message.Embeds.Where(e => e.Type == EmbedType.Video);
            if (attachments.Count() == 0 && embeds.Count() == 0)
            {
                return;
            }

            // If the message has video attachments, check how large they are.
            Dictionary<string, string> largeVideoUrls = null;
            Dictionary<string, string> smallVideoUrls = null;
            foreach (var attachment in attachments)
            {
                if (attachment.Size > maxMessageSize)
                {
                    largeVideoUrls ??= new Dictionary<string, string>();
                    largeVideoUrls.Add(attachment.Filename, attachment.Url);
                }
                else
                {
                    smallVideoUrls ??= new Dictionary<string, string>();
                    smallVideoUrls.Add(attachment.Filename, attachment.Url);
                }
            }

            // Get the filesize of each embed and add to the appropriate list.
            foreach (var embed in embeds)
            {
                var url = embed.Url;
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var size = response.Content.Headers.ContentLength;
                if (size > maxMessageSize)
                {
                    largeVideoUrls ??= new Dictionary<string, string>();
                    var filename = Path.GetFileName(url);
                    largeVideoUrls.Add(filename, url);
                }
                else
                {
                    smallVideoUrls ??= new Dictionary<string, string>();
                    var filename = Path.GetFileName(url);
                    smallVideoUrls.Add(filename, url);
                }
            }

            if (isUserInitiated && largeVideoUrls != null && smallVideoUrls == null)
            {
                await message.ReplyAsync($"All videos attached/linked were too large to analyze (Max {maxMessageSize} KiB).");
            }
            else if (!isUserInitiated && largeVideoUrls != null && smallVideoUrls == null)
            {
                // If this is not a user initiated command, and there are no small videos, then we don't want to analyze anything.
                return;
            }

            List<string> savedVideoPaths = new List<string>();
            // Create a temp folder to store the files.
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            // Analyze the small attachments.
            try
            {
                // Try to find URL results in the cache
                var cacheProvider = _cacheFactory.GetCachingProvider("VolumeAnalyzerCache");

                var results = new List<(string Filename, double Peak, double Loudness, bool error)>();
                // Download the files and analyze them.
                foreach (var videoNameUrlKv in smallVideoUrls)
                {
                    var SaveAndAnalyzeAction = async () =>
                    {
                        var path = Path.Combine(tempFolder, videoNameUrlKv.Key);
                        try
                        {
                            savedVideoPaths.Add(path);
                            var response = await _client.GetAsync(videoNameUrlKv.Value);
                            response.EnsureSuccessStatusCode();
                            using (var fileStream = File.Create(path))
                            {
                                await response.Content.CopyToAsync(fileStream);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, $"Error downloading file {videoNameUrlKv.Key}");
                            return (Path.GetFileName(path), -999, -999, true);
                        }
                        try
                        {
                            var result = AnalyzeLoudnessMetadata(path);
                            return (Path.GetFileName(path), result.MaxLevel, result.MaxLoudness, !result.success);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, $"Error analyzing file {path}");
                            return (Path.GetFileName(path), -999, -999, true);
                        }
                    };
                    var result = await cacheProvider.GetAsync<(string Filename, double Peak, double Loudness, bool error)>(videoNameUrlKv.Value, SaveAndAnalyzeAction, TimeSpan.FromDays(7));
                    results.Add(result.Value);
                    if (result.Value.error)
                    {
                        // dont cache errors
                        await cacheProvider.RemoveAsync(videoNameUrlKv.Value);
                    }
                }
                    

                // If user initiated, reply with full results
                if (isUserInitiated)
                {
                    // Build the message.
                    var sb = new StringBuilder();
                    sb.AppendLine("```");
                    sb.AppendLine("Filename, Peak, Loudness, Very Loud (>-8 LUFS)");
                    foreach (var result in results)
                    {
                        sb.Append($"{result.Filename}{(result.error?" (ERROR!)":"")}, {result.Peak.ToString("F2", CultureInfo.InvariantCulture)}, {result.Loudness.ToString("F2", CultureInfo.InvariantCulture)}");
                        if (result.Loudness >= DefaultMaxLoudness)
                        {
                            sb.Append(", 🔊");
                        }
                        sb.AppendLine();
                    }
                    if (largeVideoUrls != null)
                    {
                        sb.AppendLine("```");
                        sb.AppendLine("The following videos were too large to analyze:");
                        sb.AppendLine("```");
                        foreach (var videoNameUrlKv in largeVideoUrls)
                        {
                            sb.AppendLine(videoNameUrlKv.Key);
                        }
                    }
                    sb.AppendLine("```");

                    // Reply with the message.
                    await message.ReplyAsync(sb.ToString());
                }
                else
                {
                    var resultsThatAreTooLoud = results.Where(r => r.Loudness >= DefaultMaxLoudness);
                    if (resultsThatAreTooLoud.Count() > 0)
                    {
                        if (resultsThatAreTooLoud.Count() == attachments.Count())
                        {
                            if (attachments.Count() == 1)
                                await message.ReplyAsync("Warning! This video *may* be really *fucking* loud!");
                            else
                                await message.ReplyAsync("Warning, these videos *may* be really *fucking* loud!");
                        }
                        else
                        {
                            await message.ReplyAsync($"Warning! The videos {string.Join(", ", resultsThatAreTooLoud.Select(r => r.Filename))} *may* be really *fucking* loud!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Something went wrong while analyzing the volume of message https://discord.com/channels/{guildId}/{message.Channel.Id}/{message.Id}");
            }
            finally
            {
                // Delete the files.
                foreach (var path in savedVideoPaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, $"Could not delete {path}.");
                        }
                    }
                    else
                    {
                        _logger.Warn($"Could not delete {path}, does not exist.");
                    }
                }
                // Delete the temp folder.
                try
                {
                    Directory.Delete(tempFolder);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Could not delete {tempFolder}.");
                }
            }
        }



        /// <summary>
        /// Uses ffprobe to analyze the audio file and return the max level and max loudness.
        /// </summary>
        /// <param name="filename">Path to file for analysis.</param>
        /// <returns>The max level and max loudness of the file.</returns>
        private (double MaxLevel, double MaxLoudness, bool success) AnalyzeLoudnessMetadata(string filename)
        {
            Process p = new Process();
            p.StartInfo.FileName = "ffmpeg";
            p.StartInfo.Arguments = $"-hide_banner -nostats -i \"{filename}\" -af loudnorm=print_format=json -f null -";
            p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true; // just ffmpeg things
            p.Start();

            StringBuilder outputBuilder = new StringBuilder(); // Create a StringBuilder to accumulate the output.
            p.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data); // Append the received data to the StringBuilder.
                }
            };

            p.Start();
            p.BeginErrorReadLine(); // Start asynchronous reading of the error output.
            ulong timeoutMS = 20000;
            if (!p.WaitForExit((int)timeoutMS))
            {
                p.Kill();
                _logger.Error($"ffmpeg timed out after {timeoutMS}ms.");
                return (-999, -999, false);
            }

            p.CancelErrorRead(); // Stop asynchronous reading of the error output.
            string output = outputBuilder.ToString(); // Get the accumulated output.

            // Regex match the json
            Regex regex = new Regex(@"{[^{}]*}");
            Match match = regex.Match(output);

            if (!match.Success)
            {
                return (-999, -999, false);
            }

            // Parse the json
            LoudnormResult loudnormResult = JsonConvert.DeserializeObject<LoudnormResult>(match.Value);

            double maxLevel = loudnormResult.input_tp;
            double maxLoudness = loudnormResult.input_i;

            return (maxLevel, maxLoudness, true);
        }

    }
}
