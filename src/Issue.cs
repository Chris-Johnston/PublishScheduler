using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class Issue
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("state")] // could make this an enum
        public string State { get; set; }

        [JsonProperty("labels")]
        public List<Label> Labels { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [JsonProperty("closed_at")]
        public DateTimeOffset? ClosedAt { get; set;}

        [JsonProperty("body")]
        public string Body { get; set;}

        [JsonProperty("author_association")]
        public string AuthorAssociation { get; set; }
    }
}