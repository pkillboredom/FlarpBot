using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Discord_Bot
{
    public class Functions
    {
        private readonly IServiceProvider serviceProvider;

        public Functions(IServiceProvider _serviceProvider)
        {
            serviceProvider = _serviceProvider;
        }

        public async Task SetBotStatusAsync(DiscordSocketClient client)
        {
            var config = GetConfig();

            string currently = config["currently"].ToLower();
            string statusText = config["playing_status"];
            string onlineStatus = config["status"].ToLower();

            // Set the online status
            if (!string.IsNullOrEmpty(onlineStatus))
            {
                UserStatus userStatus = onlineStatus switch
                {
                    "dnd" => UserStatus.DoNotDisturb,
                    "idle" => UserStatus.Idle,
                    "offline" => UserStatus.Invisible,
                    _ => UserStatus.Online
                };

                await client.SetStatusAsync(userStatus);
                Console.WriteLine($"{DateTime.Now.TimeOfDay:hh\\:mm\\:ss} | Online status set | {userStatus}");
            }

            // Set the playing status
            if (!string.IsNullOrEmpty(currently) && !string.IsNullOrEmpty(statusText))
            {
                ActivityType activity = currently switch
                {
                    "listening" => ActivityType.Listening,
                    "watching" => ActivityType.Watching,
                    "streaming" => ActivityType.Streaming,
                    _ => ActivityType.Playing
                };

                await client.SetGameAsync(statusText, type: activity);
                Console.WriteLine($"{DateTime.Now.TimeOfDay:hh\\:mm\\:ss} | Playing status set | {activity}: {statusText}");
            }            
        }

        public IConfiguration GetConfig()
        {
            return serviceProvider.GetRequiredService<IConfiguration>();
        }

        public string GetAvatarUrl(SocketUser user, ushort size = 1024)
        {
            // Get user avatar and resize it. If the user has no avatar, get the default Discord avatar.
            return user.GetAvatarUrl(size: size) ?? user.GetDefaultAvatarUrl(); 
        }
    }
}
