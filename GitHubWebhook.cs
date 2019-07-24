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
using Octokit;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Xml.Serialization;

namespace PublishScheduler
{
    public static class GitHubWebhook
    {
        // gets the rest of merge data from one that was created from a tag or PR comment/body
        private static MergeData GetMergeData(this MergeData fromBody, WebhookPayload payload)
        {
            if (fromBody == null) return null; // silently fail

            fromBody.RepositoryOwner = payload.Repository.Owner.Login;
            fromBody.RepositoryName = payload.Repository.Name;
            fromBody.PullRequestNumber = payload.PullRequest?.Number ?? payload.Issue?.Number ?? -1;
            fromBody.PullRequestAuthor = payload.PullRequest?.User?.Login;
            fromBody.MergeIssuer = payload.Sender?.Login;
            fromBody.InstallationId = payload.Installation?.Id ?? 0;
            return fromBody;
        }

        private static MergeData CheckCommentHasCommand(WebhookPayload payload)
        {
            var body = payload?.Comment?.Body;
            if (body == null)
                return null;

            var parser = MergeInfoParser.GetCommentParser();
            var result = parser.Parse(body);
            return result.GetMergeData(payload);
        }

        private static MergeData CheckPRHasCommand(WebhookPayload payload)
        {
            var body = payload?.PullRequest?.Body;
            if (body == null)
                return null;

            var parser = MergeInfoParser.GetCommentParser();
            var result = parser.Parse(body);
            return result.GetMergeData(payload);
        }

        private static string InsertMessageToQueue (CloudQueue cQueueToInsert, MergeData mdMessageData, TimeSpan tsTimeToExecute)
        {
            // AddMessage has no return value, so failure will only show as exception
            try
            {
                // prep object and add to queue
                string sMessageDataJson = JsonConvert.SerializeObject(mdMessageData);
                CloudQueueMessage cqMessage = new CloudQueueMessage(sMessageDataJson);
                cQueueToInsert.AddMessage(cqMessage, null, tsTimeToExecute, null, null);

                string sResult = "Message : " + Environment.NewLine + sMessageDataJson + Environment.NewLine + " ---- for " + cqMessage.Id + " inserted into queue.";

                return sResult;
            } catch (Exception eThrownException)
            {
               return eThrownException.Message; 
            }   
        }

        // the name of the header which indicates the type of event
        private const string EventType = "X-GitHub-Event";
        
        private const string PullRequestEvent = "pull_request";
        private const string IssueCommentEvent = "issue_comment";

        private const int ngrokAppId = 36401;
        private const int prodAppId = 36392; // HACK: really should set this in env vars but I think it's fine

        public static int AppId
            => GetDebug() ? ngrokAppId : prodAppId;

        private static bool GetDebug()
        {
            // to use the ngrokAppId, set this env var to "debug"
            var environment = Environment.GetEnvironmentVariable("FUNCTION_ENVIRONMENT");
            return environment?.ToLower() == "debug";
        }

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
            var azureStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount csAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
            CloudQueueClient cQueueClient = csAccount.CreateCloudQueueClient();
            CloudQueue cQueue = cQueueClient.GetQueueReference("scheduledprsqueue");

            // this env var should have an xml body containing an RSA key
            var xmlGHPrivateKey = Environment.GetEnvironmentVariable("GitHubPrivateKey");
            var handler = new GitHubEventHandlers(log, xmlGHPrivateKey, AppId);

            // deserialize the payload
            var payload = JsonConvert.DeserializeObject<WebhookPayload>(requestBody);

            bool isHuman = payload?.Sender?.Type == "User"; // don't respond in an infinite loop

            if (payload != null && isHuman)
            {
                log.LogDebug("Deserialized the payload.");

                try
                {
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
                                // ack to the comment
                                await handler.AckAddToQueueAsync(result);
                                await handler.BlockMergeAsync(result);

                                log.LogInformation($"Got comment with command: {result.BranchName} {result.MergeTime}");
                                log.LogInformation($"Message insert result: " + InsertMessageToQueue(cQueue, result, TimeSpan.FromMinutes(5)));
                            }

                            // debug, this should be done in QueueExecutor
                            // MergePRAsync(log, xmlGHPrivateKey, result).GetAwaiter().GetResult();
                        break;
                    }
                }
                catch (Exception e)
                {
                    log.LogInformation(e, "Caught exception when processing payload.");
                }

            }
            return new OkResult();
        }
    }
}
