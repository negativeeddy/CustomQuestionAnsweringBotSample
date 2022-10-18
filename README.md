# Composer sample using Azure Language service 

## Summary
This sample shows how to add a custom recognizer to a Bot Composer empty bot in order to use [Azure Question Answering](https://azure.microsoft.com/en-us/products/cognitive-services/question-answering/) (instead of using QnA Maker), Custom Conversation Understanding, or Orchestration Workflow instead of QnAMaker, LUIS and/or Bot Orchestrator. It has not been exhaustively tested but demonstrates how a custom trigger in Composer works and this solution may need additional refinement. (also see the limitations section below)

## Quickstart
1. Install the solution's nuget pacakge to Composer
  - build the recognizer project you want to use and create a a nuget package ("Right-Click Project -> Pack" in Visual Studio)
  - Add a new source to Composer's package manager to point to the nuget directory
  - Install the package to your Composer project
2. Change the root dialog's recognizer type to custom
4. Update the settings in the custom recognizer with your keys 
3. Use custom intent triggers or the built in QnAIntent trigger as usual in the root dialog

## Settings
For Conversation Understanding or Orchestration use the CLU recognizer
```
{
    "$kind": "NegativeEddy.CluAdaptiveRecognizer",
    "projectName": "<your project name>",
    "endpoint": "<your endpoint, including https://>",
    "endpointKey": "<your endpoint key>",
    "projectType": "<CLU|Orchestration>",
    "deploymentName": "<your language deployment name>"
}
```
projectType defaults to “CLU” if not set, all other settings are required

or 

```
{
 "$kind": "NegativeEddy.CustomQuestionAnsweringRecognizer",
  "hostname": "<your endpoint, including https://>",
  "projectName": "<your project name>",
  "endpointKey": "<your endpoint key>",
}
```

Because the Question Answering recognizer is a modified version of the existing QnAMaker recognizer, the workflow elements (such as multi turn) work the same. In addition the same QnAMaker events and telemetry are written out to the logs.

The CLU/Orchestration recognizer will emit events title "CLU" events.

If using both Question Answering and Language Understanding, build an Orchestration service and use the Orchestration servuce with the CLU trigger.

## Limitations

* Composer does not integrate natively with Question Answering or the Conversation Understanding, so managing the question/answer pairs and intents must be done in the language portal instead of Composer.
* Testing must be done to check that the Question Answering score and Language scores are comparable to previous services. (There are some additional parameters that are used internally that might be useful exposed as configuration settings such as minimum scores, etc)
