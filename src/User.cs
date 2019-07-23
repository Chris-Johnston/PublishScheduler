using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class User
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}