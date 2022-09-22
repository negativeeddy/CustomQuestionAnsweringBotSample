# CustomQuestionAnsweringBotSample

## Summary
This sample shows how to add a custom recognizer to a Bot Composer empty bot in order to use [Azure Question Answering](https://azure.microsoft.com/en-us/products/cognitive-services/question-answering/) (instead of using QnA Maker). It has not been exhaustively tested but demonstrates how a custom trigger in Composer works and this solution may need additional refinement. (also see the limitations section below)

## Quickstart
[This commit](https://github.com/negativeeddy/CustomQuestionAnsweringBotSample/commit/5b5e1627d9e5621166185ce1f762a3d59a2f4cdd) shows the changes necessary to apply to a bot.
1. Add the custom recognizer class to the bot solution
2. Register the recognizer in the startup
3. Change the root dialog's recognizer type to custom
4. Update the settings in the custom recognizer with your keys 
3. Add a QnAIntent trigger to the root dialog

Because the Question Answering recognizer is a modified version of the existing QnAMaker recognizer, the workflow elements (such as multi turn) work the same. In addition the same QnAMaker events and telemetry are written out to the logs.

## Limitations
* Composer does not integrate natively with Question Answering so managing the question/answer pairs must be done in the language portal instead of Composer.
* This solution does not cross train with LUIS. If using LUIS, as shown here, the LUIS intents must also now be managed in the LUIS portal and not through Composer.
* Testing must be done to check that the Question Answering score and LUIS scores are comparable. Even though they both score 0-1.0, they score values don't necessarily correlate accross different services.

## LUIS integration

This sample shows integration with LUIS as well, but it does not do any cross training between LUIS and QuestionAnswering. The RecognizerSet simply calls both LUIS and Question Answering and then uses the highest scoring intent returned from either. In my testing, I occasionally needed to add some misrouted questions to the LUIS "none" intent to get the routing to perform correctly.

If you did not have LUIS intents to recognize, the custom recognizer definition would be just the JSON node for the Question Answering recognizer and the bot would essentially have functionality parity with QnA Maker bots.
