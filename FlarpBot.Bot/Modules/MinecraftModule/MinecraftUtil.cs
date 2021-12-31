using FlarpBot.Bot.Modules.MinecraftModule.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MineStatLib;
using NLog;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace FlarpBot.Bot.Modules.MinecraftModule
{
    internal class MinecraftUtil
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration config;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        public MinecraftUtil(IServiceProvider _serviceProvider)
        {
            serviceProvider = _serviceProvider;
            config = serviceProvider.GetRequiredService<IConfiguration>();
        }

        Regex hostRegex = new Regex(@"^(((?!\-))(xn\-\-)?[a-z0-9\-_]{0,61}[a-z0-9]{1,1}\.)*(xn\-\-)?([a-z0-9\-]{1,61}|[a-z0-9\-]{1,30})\.[a-z]{2,}$");
        public MinecraftServerStatus GetServerStatus(bool useFullMotd = false)
        {
            string targetHostString = config["minecraft:host"];
            string targetPort = config["minecraft:port"];

            // Use Default.
            if (string.IsNullOrWhiteSpace(targetPort))
            {
                targetPort = "25565";
            }

            bool hostAsDomainIsValid = !string.IsNullOrWhiteSpace(targetHostString) && hostRegex.IsMatch(targetHostString);
            IPAddress targetIP;
            ushort port = 0;
            bool hostAsIPIsValid = IPAddress.TryParse(targetHostString, out targetIP);
            bool portIsValid = !string.IsNullOrWhiteSpace(targetPort) && ushort.TryParse(targetPort, out port);
            if (!(hostAsDomainIsValid || hostAsIPIsValid) && !portIsValid)
            {
                logger.Error($"Status requested for invalid IP & Port '{targetHostString}:{targetPort}'");
                throw new InvalidOperationException("The Host and Port entered were invalid.");
            }
            if (!portIsValid)
            {
                logger.Error($"Status requested for invalid Port '{targetHostString}:{targetPort}'");
                throw new InvalidOperationException("The Port entered was invalid.");
            }
            if (!(hostAsDomainIsValid || hostAsIPIsValid))
            {
                logger.Error($"Status requested for invalid Host '{targetHostString}:{targetPort}'");
                throw new InvalidOperationException("The Host entered was invalid.");
            }

            logger.Info($"Status requested for '{targetHostString}:{targetPort}'");

            MineStat ms = new MineStat(targetHostString, port);
            bool serverUp = ms.ServerUp;
            MinecraftServerStatus status;
            if (serverUp)
            {
                string motd = useFullMotd ? ms.Motd : CleanMotdString(ms.Motd);
                status = new MinecraftServerStatus
                {
                    ServerUp = serverUp,
                    CurrentPlayers = ms.CurrentPlayersInt,
                    Version = ms.Version,
                    Host = ms.Address,
                    MaximumPlayers = ms.MaximumPlayersInt,
                    Motd = motd,
                    Port = ms.Port
                };
            }
            else
            {
                status = new MinecraftServerStatus
                {
                    ServerUp = serverUp,
                    Host = targetHostString,
                    Port = port
                };
            }

            return status;
        }

        static Regex controlCodeRegex = new Regex(@"([\xA7]\w|\\[uU]00A[A-z0-9]|\\n)");
        static Regex whiteSpaceRegex = new Regex(@"\s{2,}");
        public static string CleanMotdString(string motd)
        {
            if (string.IsNullOrWhiteSpace(motd)) return "";
            string noControlCodes = controlCodeRegex.Replace(motd, "");
            string reducedWhiteSpace = whiteSpaceRegex.Replace(noControlCodes, " ");
            return reducedWhiteSpace.Trim();
        }

    }
}
