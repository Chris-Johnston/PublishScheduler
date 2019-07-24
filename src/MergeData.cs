using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PublishScheduler
{
    [JsonObject]
    public class MergeData
    {
        [JsonProperty("merge_time")]
        public DateTime MergeTime { get; set; }

        [JsonProperty("branch_name")]
        public string BranchName { get; set; }

        [JsonProperty("repository_owner")]
        public string RepositoryOwner { get; set; }

        [JsonProperty("pull_request_number")]
        public int PullRequestNumber { get; set; }

        [JsonProperty("repository_name")]
        public string RepositoryName { get; set; }

        [JsonProperty("pull_request_author")]
        public string PullRequestAuthor { get; set; }

        [JsonProperty("merge_issuer")]
        public string MergeIssuer { get; set; }

        [JsonProperty("installation_id")]
        public int InstallationId { get; set; }
    }
}
