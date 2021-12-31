namespace FlarpBot.Bot.Models
{
    public class SayInChannelRequest
    {
        public string RequestId { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Message { get; set; }
    }
}
