using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class Installation
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}