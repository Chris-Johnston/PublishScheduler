using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
        public static async Task Run([QueueTrigger("scheduledprsqueue", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            MergeData mdQueueObject = Newtonsoft.Json.JsonConvert.DeserializeObject<MergeData>(myQueueItem);

            log.LogInformation($"Queue trigger function processed: Merge time " + mdQueueObject.MergeTime + " and branch name " + mdQueueObject.BranchName);

            // this env var should have an xml body containing an RSA key
            var xmlGHPrivateKey = Environment.GetEnvironmentVariable("GitHubPrivateKey");
            var handler = new GitHubEventHandlers(log, xmlGHPrivateKey, GitHubWebhook.AppId);

            try
            {
                await handler.MergePRAsync(mdQueueObject);
                if (!string.IsNullOrWhiteSpace(mdQueueObject.BranchName))
                {
                    // branchname specified, then create new PR from this PR's dest to the specified dest
                    await handler.CreateNewPR(mdQueueObject);
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e, "Caught exception when handling queue item.");
            }
        }
    }
}
