using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlarpBot.Bot.Modules.GarrysModule
{
    internal class GmodUtil
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration config;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Regex CombatLogRegex = new Regex(@"(?'Date'\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3}): (?'Player'.*) \((?'PlayerKarma'[\d.]*)\) (?'Action'\S+) (?'Target'.*) \((?'TargetKarma'[\d.]*)\) and gets (?'PenaltyOrReward'\w+)[\D]*(?'PenaltyOrRewardAmount'[\d.]*)");
        public GmodUtil(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            config = serviceProvider.GetRequiredService<IConfiguration>();
        }

        public string GetCombatLogLastTen()
        {
            var combatLogPath = config["gmod:combatLogPath"];
            var mostRecentFile = GetMostRecentFile(combatLogPath);
            return GetLastLines(mostRecentFile, 10);
        }

        private string GetLastLines(string filePath, long numLines)
        {
            var lines = new LinkedList<string>();
            int count = 0;

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(file))
                {
                    var line = reader.ReadLine();
                    while (line != null)
                    {
                        // Ignore all but matching lines.
                        if (CombatLogRegex.IsMatch(line))
                        {
                            if (count++ >= numLines)
                            {
                                lines.RemoveFirst();
                            }
                            lines.AddLast(line);
                        }
                        line = reader.ReadLine();
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string GetMostRecentFile(string directoryPath)
        {
            string mostRecentFile = null;
            try
            {
                var files = Directory.GetFiles(directoryPath);
                DateTime mostRecentDate = DateTime.MinValue;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime > mostRecentDate)
                    {
                        mostRecentFile = file;
                        mostRecentDate = fileInfo.LastWriteTime;
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
            }

            return mostRecentFile;
        }
    }
}
