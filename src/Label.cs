using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class Label
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("default")]
        public bool Default { get; set; }
    }
}