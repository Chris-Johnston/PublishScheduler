using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace PublishScheduler
{
    // logic surrounding different types of events in GitHub
    // this should contain the handler used by queueexecutor that will perform the checks
    // and merge the PR
    public class GitHubEventHandlers
    {
        private readonly Microsoft.Extensions.Logging.ILogger log;
        private readonly string privateKeyXML;
        private readonly int appId;
        
        public GitHubEventHandlers(Microsoft.Extensions.Logging.ILogger log, string privateKeyXML, int appId)
        {
            this.log = log;
            this.privateKeyXML = privateKeyXML;
            this.appId = appId;
        }

        private string GetGitHubJWT(string privateKeyXML, int installationId)
        {
            log.LogInformation($"Getting JWT for installation id: {installationId}");
            var provider = new RSACryptoServiceProvider();
            var issuedAt = DateTime.UtcNow;
            var expires = DateTime.UtcNow.AddMinutes(10);
            RSAKeyValue rsaKeyValue;

            // deserialize the privateKeyXML as a RSAKeyValue
            var ser = new XmlSerializer(typeof(RSAKeyValue));
            using (var reader = new StringReader(privateKeyXML))
            {
                rsaKeyValue = ser.Deserialize(reader) as RSAKeyValue;
            }

            // provide those parameters to the RSACryptoServiceProvider
            // use ToRSAParameter to convert form base64 strings to
            // byte[] RSAParams
            provider.ImportParameters(rsaKeyValue.ToRSAParameters());
            var key = new RsaSecurityKey(provider);

            // actually create the token (hooray!)
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken($"{appId}", null, null, null, expires, issuedAt,
                new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

            var jwt = token.RawData;

            log.LogInformation($"jwt: {jwt}");

            return jwt;
        }

        private async Task<GitHubClient> GetInstallationClientAsync(int installationId)
        {
            log.LogInformation($"Setting up client for installation ID: {installationId}");
            var jwt = GetGitHubJWT(privateKeyXML, installationId);
            var header = new Octokit.ProductHeaderValue("PublishScheduler", "0.0.1");

            var appClient = new GitHubClient(header)
            {
                Credentials = new Credentials(jwt, AuthenticationType.Bearer)
            };

            log.LogInformation("Getting installation token.");

            // TODO: get installation ID from webhook payload

            // DEBUG: list all installation ids
            var installations = await appClient.GitHubApps.GetAllInstallationsForCurrent();
            foreach (var i in installations)
            {
                log.LogInformation($"installation: {i.Id} {i.HtmlUrl}");
            }

            var response = await appClient.GitHubApps.CreateInstallationToken(installationId);

            log.LogInformation($"Creating client for installation {installationId}");
            var client = new GitHubClient(header)
            {
                Credentials = new Credentials(response.Token)
            };
            return client;
        }

        public async Task AckAddToQueueAsync(MergeData data)
        {
            var client = await GetInstallationClientAsync(data.InstallationId);
            log.LogInformation($"ACKing to PR comment {data.RepositoryOwner}/{data.RepositoryName}#{data.PullRequestNumber}");
            
            var message = $"Ok @{data.MergeIssuer} , I'll merge this Pull Request at `{data.MergeTime}` UTC + about 5 minutes. (Currently it's `{DateTime.UtcNow}` UTC.)";
            await client.Issue.Comment.Create(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber, message);
        }

        // creates a new pr to the target branch after the old pr is merged
        public async Task CreateNewPR(MergeData data)
        {
            var client = await GetInstallationClientAsync(data.InstallationId);

            // check that the pr went through first
            var mergedPR = await client.PullRequest.Get(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber);
            if (!mergedPR.Merged)
            {
                log.LogError("The pull request was not merged yet.");
                return;
            }

            var sourceBranch = mergedPR.Base.Ref; // this could? be a commit hash too
            var pr = new NewPullRequest($"AutoMerge: {sourceBranch} to {data.BranchName} by @{data.MergeIssuer}", sourceBranch, data.BranchName);
            log.LogInformation($"Creating new PR: {sourceBranch} to {data.BranchName}");
            var result = await client.PullRequest.Create(data.RepositoryOwner, data.RepositoryName, pr);

            // assign ownership
            log.LogInformation($"Created PR {result.Number}. Assigning ownership to {data.MergeIssuer}");
            var reviewReq = new PullRequestReviewRequest(new List<string>{ data.MergeIssuer });
            await client.PullRequest.ReviewRequest.Create(data.RepositoryOwner, data.RepositoryName, result.Number, reviewReq);

            // TODO handle invalid BranchName

            log.LogInformation("Leaving a comment on closed PR.");
            // leave a comment on the merged PR
            var comment = $"Ok @{data.MergeIssuer}, I've created Pull Request #{result.Number} for you that merges `{sourceBranch}` into `{data.BranchName}`.";
            await client.Issue.Comment.Create(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber, comment);
        }

        public async Task BlockMergeAsync(MergeData data)
        {
            var client = await GetInstallationClientAsync(data.InstallationId);

            var pr = await client.PullRequest.Get(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber);
            var prHead = pr.Head;

            var checkRun = new NewCheckRun($"PublishingScheduler Auto-Merge", prHead.Ref);
            checkRun.Status = CheckStatus.Completed;
            checkRun.Conclusion = CheckConclusion.Failure;
            checkRun.Output = new NewCheckRunOutput($"Auto-Merge at {data.MergeTime} UTC", $"An auto-merge is scheduled for {data.MergeTime} UTC.");
            await client.Check.Run.Create(data.RepositoryOwner, data.RepositoryName, checkRun);
        }

        public async Task PassMergeAsync(MergeData data)
        {
            var client = await GetInstallationClientAsync(data.InstallationId);

            var pr = await client.PullRequest.Get(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber);
            var prHead = pr.Head;

            var checkRun = new NewCheckRun($"PublishingScheduler Auto-Merge", prHead.Ref);
            checkRun.Status = CheckStatus.Completed;
            checkRun.Conclusion = CheckConclusion.Success;
            checkRun.Output = new NewCheckRunOutput($"{data.MergeTime} UTC", $"An auto-merge is scheduled for {data.MergeTime} UTC.");
            await client.Check.Run.Create(data.RepositoryOwner, data.RepositoryName, checkRun);
        }

        public async Task MergePRAsync(MergeData data)
        {
            var client = await GetInstallationClientAsync(data.InstallationId);

            log.LogInformation($"Merging Pull Request: {data.RepositoryOwner}/{data.RepositoryName}#{data.PullRequestNumber}");

            var x = await client.PullRequest.Get(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber);
            log.LogInformation($"PR #{data.PullRequestNumber} mergeable status {x.Mergeable} {x.State}");

            
            // check requester association
            var valid = new List<string>(){ "MEMBER", "COLLABORATOR", "OWNER" };

            if (!valid.Contains(data.RequesterAssociation))
            {
                // user is not allowed to do this
                await client.Issue.Comment.Create(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber, $"Auto-merge blocked, user @{data.MergeIssuer} ({data.RequesterAssociation}) does not have the association required to merge.");
                return;
            }

            if (x.Mergeable == false)
            {
                await client.Issue.Comment.Create(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber, $"Auto-merge blocked by unmergeable state. @{data.MergeIssuer}, please resolve this and merge manually.");
            }
            else
            {
                await client.PullRequest.Merge(data.RepositoryOwner, data.RepositoryName, data.PullRequestNumber,
                    new MergePullRequest()
                    {
                        // AutoMerge #123 from Chris-Johnston: Implement the thing and solve world hunger
                        CommitTitle = $"AutoMerge #{data.PullRequestNumber} from {data.MergeIssuer}: {x.Title}",
                        MergeMethod = PullRequestMergeMethod.Squash, // HACK: need to check which merge methods are allowed by the repo and pick one that will work.
                    });
            }
        }
    }
}