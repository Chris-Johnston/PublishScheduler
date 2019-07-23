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
        private static MergeData CheckCommentHasCommand(WebhookPayload payload)
        {
            var body = payload?.Comment?.Body;
            if (body == null)
                return null;

            var parser = MergeInfoParser.GetCommentParser();
            var result = parser.Parse(body);
            return result;
        }

        private static MergeData CheckPRHasCommand(WebhookPayload payload)
        {
            var body = payload?.PullRequest?.Body;
            if (body == null)
                return null;

            var parser = MergeInfoParser.GetCommentParser();
            var result = parser.Parse(body);
            return result;
        }

        private static string InsertMessageToQueue (CloudQueue cQueueToInsert, MergeData mdMessageData, TimeSpan tsTimeToExecute)
        {
            try
            {
                CloudQueueMessage cqMessage = new CloudQueueMessage((mdMessageData).ToString());
                cQueueToInsert.AddMessage(cqMessage, null, tsTimeToExecute, null, null);
                return "Message " + cqMessage.Id + " inserted into queue.";
            } catch (Exception eThrownException)
            {
               return eThrownException.Message; 
            }   
        }

        // the name of the header which indicates the type of event
        private const string EventType = "X-GitHub-Event";
        
        private const string PullRequestEvent = "pull_request";
        private const string IssueCommentEvent = "issue_comment";

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

            // Establishing connectivity to queue
            CloudStorageAccount csAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pubscheda792;AccountKey=x5C8PUwhSi94mgV2HALD6oGHA0sAGMq408OAz1xSXjHudGdi5nDsG3NQTIVmV/1d2hYN1uRwJhhrGSiuYUqQkA==;");
            CloudQueueClient cQueueClient = csAccount.CreateCloudQueueClient();
            CloudQueue cQueue = cQueueClient.GetQueueReference("scheduledprsqueue");

            // deserialize the payload
            var payload = JsonConvert.DeserializeObject<WebhookPayload>(requestBody);
            if (payload != null)
            {
                log.LogDebug("Deserialized the payload.");
                
                switch (eventType)
                {
                    case PullRequestEvent:
                        var prresult = CheckPRHasCommand(payload);
                        if (prresult != null)
                        {
                            log.LogInformation($"Got PR with command: {prresult.BranchName} {prresult.MergeTime}");
                            log.LogInformation($"Message insert result: " + InsertMessageToQueue(cQueue, prresult, TimeSpan.FromMinutes(5)));
                        }
                    break;
                    case IssueCommentEvent: // same event as a PR comment, need to check that this comment is made on a PR and not an issue
                        // do a thing, but differently
                        var result = CheckCommentHasCommand(payload);
                        if (result != null)
                        {
                            log.LogInformation($"Got comment with command: {result.BranchName} {result.MergeTime}");
                            log.LogInformation($"Message insert result: " + InsertMessageToQueue(cQueue, result, TimeSpan.FromMinutes(5)));
                        }
                    break;
                }

            }
            return new OkResult();
        }
    }
}
