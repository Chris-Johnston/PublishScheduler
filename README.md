# Publish Scheduler

A GitHub App that schedules your Pull Request merges. Built for the 2019 Microsoft Hackathon.

**This is a hackathon project, and is not suitable for production use.**

## Instructions

Getting started with this App is easy.

1. Add the bot to your Repository. You'll need to allow it some permissions so it can merge for you.
2. Comment in an open PR: `@publishscheduler 7/30/2019,6:00` (time is in UTC)
3. Wait for your Pull Request to be automatically merged.

If you have a more complex branching strategy, you can also specify a target branch for another Pull Request.
The bot can merge your Pull Request from `feature/123` to `master`, then create a new PR for `master` to `live`.

This is done with the `@publishscheduler 7/30/2019,6:00 live` command.

## Process

This project is using Azure Funtions. The process of accepting a comment from GitHub to merging a PR
is roughly like this:

1. GitHub sends a WebHook to our GitHub Webhook endpoint in Azure Functions.
2. The payload is parsed for the correct type of message.
3. A comment is made on the Pull Request, acknowledging the command. The app will "fail" the build, preventing an accidental manual merge.
4. JSON data is placed into a queue, with the time of when the Pull Request should be merged.
5. Items from the queue call the QueueExecutor function.
6. Checks are performed, then the app "passes" the build, merges the PR.
7. If a "next" branch was specified, a new Pull Request is created, and a comment is left on the old Pull Request.

## Try It Now

[You can add this App to your repos by visiting here.](https://github.com/apps/publishingscheduler)

**This is a hackathon project, and is not suitable for production use.**

## Deploying

This repository's `master` branch is automatically deployed to Azure Functions.