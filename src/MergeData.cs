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
        public DateTime MergeTime { get;set; }

        [JsonProperty("branch_name")]
        public string BranchName { get; set; }
    }
}
