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
        public static void Run([QueueTrigger("scheduledprsqueue", Connection = "DefaultEndpointsProtocol=https;AccountName=pubscheda792;AccountKey=x5C8PUwhSi94mgV2HALD6oGHA0sAGMq408OAz1xSXjHudGdi5nDsG3NQTIVmV/1d2hYN1uRwJhhrGSiuYUqQkA==;")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
