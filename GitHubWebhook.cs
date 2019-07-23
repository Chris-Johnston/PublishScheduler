using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PublishScheduler
{
    public static class GitHubWebhook
    {
        // the name of the header which indicates the type of event
        private const string EventType = "X-GitHub-Event";
        
        private const string PullRequestEvent = "pull_request";
        private const string IssueComentEvent = "issue_comment";



        [FunctionName("GitHubWebhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Got GitHub Webhook.");

            string eventType = string.Empty;

            if (req.Headers.ContainsKey(EventType))
            {
                eventType = req.Headers[EventType];
                log.LogInformation($"Rec. event of type: {eventType}");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogInformation($"Request body: {requestBody}");

            CloudStorageAccount csAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pubscheda792;AccountKey=x5C8PUwhSi94mgV2HALD6oGHA0sAGMq408OAz1xSXjHudGdi5nDsG3NQTIVmV/1d2hYN1uRwJhhrGSiuYUqQkA==;");
            CloudQueueClient cQueueClient = csAccount.CreateCloudQueueClient();
            CloudQueue cQueue = cQueueClient.GetQueueReference("scheduledprsqueue");
            CloudQueueMessage cqMessage = new CloudQueueMessage((req.Headers[EventType]).ToString());
            cQueue.AddMessage(cqMessage, null, TimeSpan.FromSeconds(300), null, null);

            switch (eventType)
            {
                case PullRequestEvent:
                    // do a thing
                break;
                case IssueComentEvent: // same event as a PR comment, need to check that this comment is made on a PR and not an issue
                    // do a thing, but differently
                break;
            }

            return new OkResult();
        }
    }
}
