using System;
using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class Comment 
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("author_association")]
        public string AuthorAssociation { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}