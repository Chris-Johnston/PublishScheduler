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

        

        private async static Task MergePRAsync(ILogger log, string privateKeyXML)
        {
            var header = new ProductHeaderValue("PublishScheduler", "0.0.1");

            var issuedAt = DateTime.UtcNow;
            var expires = DateTime.UtcNow.AddMinutes(10);

            try
            {
            var provider = new RSACryptoServiceProvider();

            RSAKeyValue rsaKeyValue;

            var ser = new XmlSerializer(typeof(RSAKeyValue));
            using (var reader = new StringReader(privateKeyXML))
            {
                rsaKeyValue = ser.Deserialize(reader) as RSAKeyValue;
            }
    
            provider.ImportParameters(rsaKeyValue.ToRSAParameters());
            var key = new RsaSecurityKey(provider);
            // provider.FromXmlString(xml);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken("36401", null, null, null, expires, issuedAt,
                new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

            var jwt = token.RawData;

            log.LogInformation($"got jwt: {jwt}");




            // do something with the jwt

            var appClient = new GitHubClient(header)
            {
                Credentials = new Credentials(jwt, AuthenticationType.Bearer)
            };

            // TODO: get installation ID from webhook payload
            var installation = await appClient.GitHubApps.GetInstallationForCurrent(1314101);
            var response = await appClient.GitHubApps.CreateInstallationToken(1314101);

            var client = new GitHubClient(header)
            {
                Credentials = new Credentials(response.Token)
            };

            var x = await client.PullRequest.Get("Chris-Johnston", "testscheduler", 2);
            log.LogInformation($"PR mergeable {x.Mergeable} {x.State}");

            if (x.Mergeable == false)
            {
                await client.Issue.Comment.Create("Chris-Johnston", "testscheduler", 2, "Auto-merge blocked by unmergeable state.");
            }
            else
            {
                await client.PullRequest.Merge("Chris-Johnston", "testscheduler", 2,
                    new MergePullRequest()
                    {
                        CommitTitle = "Merge the thing.",
                        MergeMethod = PullRequestMergeMethod.Squash,
                    });
            }

            } catch (Exception e)
            {
                log.LogError(e, "caught exception while doing jwt stuff");
                log.LogDebug(e, "debug");
                log.LogInformation($"{e.ToString()}");
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

                        var xmlGHPrivateKey = Environment.GetEnvironmentVariable("GitHubPrivateKey");

                        MergePRAsync(log, xmlGHPrivateKey).GetAwaiter().GetResult();
                    break;
                }

            }
            return new OkResult();
        }
    }
}
