using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class Repository
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("private")]
        public bool Private { get; set; }

        [JsonProperty("owner")]
        public User Owner { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("default_branch")]
        public string DefaultBranch { get; set; }
    }
}