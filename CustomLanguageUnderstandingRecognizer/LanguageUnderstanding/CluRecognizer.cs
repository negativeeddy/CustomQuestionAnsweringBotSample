using Azure;
using Azure.AI.Language.Conversations;
using Azure.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureLanguageServiceRecognizers.LanguageUnderstanding
{
    /// <summary>
    /// Class for a recognizer that utilizes the CLU service.
    /// </summary>
    public class CluRecognizer : ITelemetryRecognizer
    {
        /// <summary>
        /// The declarative type for this recognizer.
        /// </summary>
        [JsonProperty("$kind")]
        public const string DeclarativeType = "AzureLanguageServiceRecognizers.LanguageUnderstanding.CluRecognizer";


        /// <summary>
        /// The value type for a LUIS trace activity.
        /// </summary>
        public const string LuisTraceType = "https://www.luis.ai/schemas/trace";

        /// <summary>
        /// The context label for a LUIS trace activity.
        /// </summary>
        private const string CluTraceLabel = "CLU Trace";
        private const string CluTraceName = "CLU Recognizer";
        private readonly string _cacheKey;

        private readonly CluRecognizerOptions _options;

        private readonly CluProjectSettings _project;

        private readonly ConversationAnalysisClient _conversationsClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisRecognizer"/> class.
        /// </summary>
        /// <param name="recognizerOptions"> The LUIS recognizer version options.</param>
        /// <param name="clientHandler">(Optional) Custom handler for LUIS API calls to allow mocking.</param>
        public CluRecognizer(CluRecognizerOptions recognizerOptions, CluProjectSettings project, ConversationAnalysisClient conversationAnalysisClient = default)
        {
            _project = project;
            _options = recognizerOptions;

            _conversationsClient = conversationAnalysisClient ?? new ConversationAnalysisClient(
                new Uri(_project.Endpoint),
                new AzureKeyCredential(_project.EndpointKey),
                _options.ApiVersion is null ? new ConversationsClientOptions() : new ConversationsClientOptions(_options.ApiVersion.Value));


            TelemetryClient = _options.TelemetryClient;
            LogPersonalInformation = _options.LogPersonalInformation;

            _cacheKey = _project.Endpoint + _project.ProjectName;
        }


        /// <summary>
        /// Gets or sets a value indicating whether to log personal information that came from the user to telemetry.
        /// </summary>
        /// <value>If true, personal information is logged to Telemetry; otherwise the properties will be filtered.</value>
        public bool LogPersonalInformation { get; set; }

        /// <summary>
        /// Gets the currently configured <see cref="IBotTelemetryClient"/> that logs the LuisResult event.
        /// </summary>
        /// <value>The <see cref="IBotTelemetryClient"/> being used to log events.</value>
        [JsonIgnore]
        public IBotTelemetryClient TelemetryClient { get; }



        /// <inheritdoc />
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await RecognizeInternalAsync(turnContext, null, null, null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="dialogContext">dialogcontext.</param>
        /// <param name="activity">activity.</param>
        /// <param name="cancellationToken">cancellationtoken.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken)
            => await RecognizeInternalAsync(dialogContext, activity, null, null, null, cancellationToken).ConfigureAwait(false);


        /// <inheritdoc />
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(turnContext, null, null, null, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">type of result.</typeparam>
        /// <param name="dialogContext">dialogContext.</param>
        /// <param name="activity">activity.</param>
        /// <param name="cancellationToken">cancellationToken.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(dialogContext, activity, null, null, null, cancellationToken).ConfigureAwait(false));
            return result;
        }


        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
        => await RecognizeInternalAsync(turnContext, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
        => await RecognizeInternalAsync(dialogContext, activity, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(turnContext, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(dialogContext, activity, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CluRecognizerOptions recognizerOptions, CancellationToken cancellationToken)
        {
            return await RecognizeInternalAsync(turnContext, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="dialogContext">dialog context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptions recognizerOptions, CancellationToken cancellationToken)
        {
            return await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CluRecognizerOptions recognizerOptions, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(turnContext, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">dialog context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CluRecognizerOptions recognizerOptions, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CluRecognizerOptions recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
        {
            return await RecognizeInternalAsync(turnContext, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptions recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
        {
            return await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CluRecognizerOptions recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(turnContext, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CluRecognizerOptions recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();
            result.Convert(await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));
            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <remarks>No telemetry is provided when using this method.</remarks>
        /// <param name="utterance">utterance to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptions"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptions"/> passed in the constructor.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The LUIS results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(string utterance, CluRecognizerOptions recognizerOptions = null, CancellationToken cancellationToken = default)
        {
            recognizerOptions ??= _options;
            return await RecognizeInternalAsync(utterance, recognizerOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invoked prior to a LuisResult being logged.
        /// </summary>
        /// <param name="recognizerResult">The Luis Results for the call.</param>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><see cref="Task"/>.</returns>
        protected virtual async Task OnRecognizerResultAsync(RecognizerResult recognizerResult, ITurnContext turnContext, Dictionary<string, string> telemetryProperties = null, Dictionary<string, double> telemetryMetrics = null, CancellationToken cancellationToken = default)
        {
            var properties = await FillLuisEventPropertiesAsync(recognizerResult, turnContext, telemetryProperties, cancellationToken).ConfigureAwait(false);

            // Track the event
            _options.TelemetryClient.TrackEvent(LuisTelemetryConstants.LuisResult, properties, telemetryMetrics);
        }

        /// <summary>
        /// Fills the event properties for LuisResult event for telemetry.
        /// These properties are logged when the recognizer is called.
        /// </summary>
        /// <param name="recognizerResult">Last activity sent from user.</param>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// additionalProperties
        /// <returns>A dictionary that is sent as "Properties" to IBotTelemetryClient.TrackEvent method for the BotMessageSend event.</returns>
#pragma warning disable CA1801 // Review unused parameters (we can't remove cancellationToken without breaking binary compat).
        protected Task<Dictionary<string, string>> FillLuisEventPropertiesAsync(RecognizerResult recognizerResult, ITurnContext turnContext, Dictionary<string, string> telemetryProperties = null, CancellationToken cancellationToken = default)
#pragma warning restore CA1801 // Review unused parameters
        {
            var topTwoIntents = recognizerResult.Intents.Count > 0 ? recognizerResult.Intents.OrderByDescending(x => x.Value.Score).Take(2).ToArray() : null;

            // Add the intent score and conversation id properties
            var properties = new Dictionary<string, string>()
            {
                { LuisTelemetryConstants.ApplicationIdProperty, _project.ProjectName },
                { LuisTelemetryConstants.IntentProperty, topTwoIntents?[0].Key ?? string.Empty },
                { LuisTelemetryConstants.IntentScoreProperty, topTwoIntents?[0].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" },
                { LuisTelemetryConstants.Intent2Property, topTwoIntents?.Length > 1 ? topTwoIntents?[1].Key ?? string.Empty : string.Empty },
                { LuisTelemetryConstants.IntentScore2Property, topTwoIntents?.Length > 1 ? topTwoIntents?[1].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" : "0.00" },
                { LuisTelemetryConstants.FromIdProperty, turnContext.Activity.From.Id },
            };

            if (recognizerResult.Properties.TryGetValue("sentiment", out var sentiment) && sentiment is JObject)
            {
                if (((JObject)sentiment).TryGetValue("label", out var label))
                {
                    properties.Add(LuisTelemetryConstants.SentimentLabelProperty, label.Value<string>());
                }

                if (((JObject)sentiment).TryGetValue("score", out var score))
                {
                    properties.Add(LuisTelemetryConstants.SentimentScoreProperty, score.Value<string>());
                }
            }

            var entities = recognizerResult.Entities?.ToString();
            properties.Add(LuisTelemetryConstants.EntitiesProperty, entities);

            // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example
            if (LogPersonalInformation && !string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                properties.Add(LuisTelemetryConstants.QuestionProperty, turnContext.Activity.Text);
            }

            // Additional Properties can override "stock" properties.
            if (telemetryProperties != null)
            {
                return Task.FromResult(telemetryProperties.Concat(properties)
                           .GroupBy(kv => kv.Key)
                           .ToDictionary(g => g.Key, g => g.First().Value));
            }

            return Task.FromResult(properties);
        }

        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="turnContext">Dialog turn Context.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="telemetryProperties"> Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, CluRecognizerOptions predictionOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _options;
            var cached = turnContext.TurnState.Get<RecognizerResult>(_cacheKey);
            if (cached == null)
            {
                var result = await RecognizeInternalAsync(turnContext, turnContext.Activity.Text, cancellationToken).ConfigureAwait(false);
                await OnRecognizerResultAsync(result, turnContext, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
                turnContext.TurnState.Set(_cacheKey, result);
                _options.TelemetryClient.TrackEvent("Luis result cached", telemetryProperties, telemetryMetrics);

                return result;
            }

            _options.TelemetryClient.TrackEvent("Read from cached Luis result", telemetryProperties, telemetryMetrics);
            return cached;
        }

        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="dialogContext">Dialog turn Context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="telemetryProperties"> Additional properties to be logged to telemetry with the LuisResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the LuisResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptions predictionOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double> telemetryMetrics, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _options;
            var turnContext = dialogContext.Context;
            var cached = turnContext.TurnState.Get<RecognizerResult>(_cacheKey);
            if (cached == null)
            {
                var result = await RecognizeInternalAsync(dialogContext, activity, cancellationToken).ConfigureAwait(false);
                await OnRecognizerResultAsync(result, dialogContext.Context, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
                turnContext.TurnState.Set(_cacheKey, result);
                _options.TelemetryClient.TrackEvent("Luis result cached", telemetryProperties, telemetryMetrics);
                return result;
            }

            _options.TelemetryClient.TrackEvent("Read from cached Luis result", telemetryProperties, telemetryMetrics);
            return cached;
        }

        private async Task<RecognizerResult> RecognizeInternalAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken)
        {
            return await RecognizeAsync(dialogContext.Context, activity?.Text, cancellationToken).ConfigureAwait(false);
        }

        private async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, string utterance, CancellationToken cancellationToken)
        {
            RecognizerResult recognizerResult;

            if (string.IsNullOrWhiteSpace(utterance))
            {
                recognizerResult = new RecognizerResult
                {
                    Text = utterance
                };
            }
            else
            {
                recognizerResult = await RecognizeInternalAsync(turnContext, utterance, cancellationToken).ConfigureAwait(false);
            }

            return recognizerResult;
        }


        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="utterance">utterance to recognize.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(string utterance, CluRecognizerOptions predictionOptions, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _options;
            var result = await RecognizeInternalAsync(utterance, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private Task<RecognizerResult> RecognizeInternalAsync(string utterance, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, string utterance, CancellationToken cancellationToken)
        {
            var data = new ConversationalTask
            {
                analysisInput = new ConversationAnalysisOptions
                {
                    conversationItem = new ConversationItemBase
                    {
                        text = utterance,
                        id = "1",
                        participantId = "1",
                        language = _options.Language,
                    }
                },
                parameters = new ConversationTaskParameters
                {
                    projectName = _project.ProjectName,
                    deploymentName = _project.DeploymentName,

                    // Use Utf16CodeUnit for strings in .NET.
                    stringIndexType = "Utf16CodeUnit",
                    isLoggingEnabled = _options.IsLoggingEnabled ?? false,
                    verbose = _options.Verbose ?? false,
                },
                kind = "Conversation",
            };

            RequestContent request = RequestContent.Create(data);
            using (var cluResponse = await _conversationsClient.AnalyzeConversationAsync(request, new RequestContext() { CancellationToken = cancellationToken }))
            {
                RecognizerResult recognizerResult = _project.ProjectType switch
                {
                    CluProjectSettings.ProjectType_CLU => await RecognizerResultBuilder.BuildRecognizerResultFromCluResponse(cluResponse.ContentStream, utterance),
                    CluProjectSettings.ProjectType_Orchestration => await RecognizerResultBuilder.BuildRecognizerResultFromOrchestratorResponse(cluResponse.ContentStream, utterance),
                    _ => throw new NotImplementedException($"unknown project type {_project.ProjectType}")
                };

                var traceInfo = JObject.FromObject(
                new
                {
                    response = new
                    {
                        cluResponse.Status,
                        cluResponse.ReasonPhrase
                    },
                    recognizerResult,
                });

                await turnContext.TraceActivityAsync(CluTraceName, traceInfo, nameof(CluRecognizer), CluTraceLabel, cancellationToken);

                return recognizerResult;
            }
        }
    }
}
