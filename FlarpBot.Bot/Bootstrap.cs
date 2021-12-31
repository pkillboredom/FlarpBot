using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.IO;
using System.Reflection;

namespace FlarpBot.Bot
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
                    .AddSingleton<ExternalRequestHandler>()
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