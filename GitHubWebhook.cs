using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
                        }
                    break;
                    case IssueComentEvent: // same event as a PR comment, need to check that this comment is made on a PR and not an issue
                        // do a thing, but differently
                        var result = CheckCommentHasCommand(payload);
                        if (result != null)
                        {
                            log.LogInformation($"Got comment with command: {result.BranchName} {result.MergeTime}");
                        }
                    break;
                }

            }
            return new OkResult();
        }
    }
}
