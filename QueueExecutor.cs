using System;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace PublishScheduler
{
    public static class QueueExecutor
    {
        [FunctionName("QueueExecutor")]
        public static void Run([QueueTrigger("scheduledprsqueue", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
