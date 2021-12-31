using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Reflection;
using NLog;

namespace Discord_Bot
{
    public static class Bootstrap
    {
        public static DiscordBot CreateDiscordBot()
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString())
                    .AddJsonFile("config.json", optional: false);

                IConfiguration config = configBuilder.Build();

                var services = new ServiceCollection()
                    .AddSingleton(config)
                    .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                    {
                        MessageCacheSize = 500,
                        LogLevel = LogSeverity.Info
                    }))
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        LogLevel = LogSeverity.Info,
                        DefaultRunMode = RunMode.Async,
                        CaseSensitiveCommands = false
                    }))
                    .AddSingleton<CommandHandlingService>()
                    .AddSingleton<Functions>()
                    //.AddSingleton(typeof(Modules.MinecraftModule.MinecraftUtil))
                    .BuildServiceProvider();

                return new DiscordBot(services);
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }

        }

        
    }
}