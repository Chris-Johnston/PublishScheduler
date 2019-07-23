using System;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PublishScheduler
{
    public static class QueueExecutor
    {
        [FunctionName("QueueExecutor")]
        public static void Run([QueueTrigger("scheduledprsqueue", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            MergeData mdQueueObject = Newtonsoft.Json.JsonConvert.DeserializeObject<MergeData>(myQueueItem);

            log.LogInformation($"Queue trigger function processed: Merge time " + mdQueueObject.MergeTime + " and branch name " + mdQueueObject.BranchName);
        }
    }
}
