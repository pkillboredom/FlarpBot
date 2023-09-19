using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FlarpBot.Bot.Modules.VolumeModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace FlarpBot.Bot
{
    public static class Bootstrap
    {
        public static DiscordBot CreateDiscordBot(IConfiguration configuration)
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                string directory;
                if (string.IsNullOrWhiteSpace(Assembly.GetEntryAssembly().Location))
                {
                    // Quick fix for publish build.
                    directory = Directory.GetCurrentDirectory();
                }
                else directory = Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString();

                var services = new ServiceCollection()
                    .AddSingleton(configuration)
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
                    .AddLogging(loggingBuilder => loggingBuilder.AddNLog("nlog.config"))
                    .AddHttpClient()
                    .AddEasyCaching(options =>
                    {
                        options.UseSQLite(config =>
                        {
                            config.DBConfig = new EasyCaching.SQLite.SQLiteDBOptions
                            {
                                FileName = Path.Combine(directory, "VolumeAnalyzerCache.db"),
                                CacheMode = Microsoft.Data.Sqlite.SqliteCacheMode.Default,
                                OpenMode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
                            };
                        }, "VolumeAnalyzerCache");
                    })
                    .AddTransient<VolumeAnalyzer>()
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