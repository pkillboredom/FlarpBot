namespace FlarpBot.Bot.Models
{
    public class ExternalRequestHandlerResponse
    {
        public string RequestId { get; set; }
        public RequestStatus RequestStatus { get; set; }
        public string RequestMessage { get; set; }
    }

    public enum RequestStatus { Success, Error }
}
