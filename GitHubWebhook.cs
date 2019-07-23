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


            switch (eventType)
            {
                case PullRequestEvent:
                    // do a thing
                break;
                case IssueComentEvent: // same event as a PR comment, need to check that this comment is made on a PR and not an issue
                    // do a thing, but differently
                break;
            }



            return new OkObjectResult("OK");
        }
    }
}
