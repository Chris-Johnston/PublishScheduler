using Newtonsoft.Json;

namespace PublishScheduler
{
    [JsonObject]
    public class WebhookPayload
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("issue")]
        public Issue Issue { get; set; }

        [JsonProperty("pull_request")]
        public Issue PullRequest { get; set; } // not a bug, this is used when PRs are created

        [JsonProperty("comment")]
        public Comment Comment { get; set; }

        [JsonProperty("sender")]
        public User Sender { get; set; }

        [JsonProperty("repository")]
        public Repository Repository { get; set; }

        [JsonProperty("installation")]
        public Installation Installation { get; set; }
    }
}