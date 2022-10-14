using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureLanguageServiceRecognizers.LanguageUnderstanding
{
    /// <summary>
    /// Class that represents an adaptive LUIS recognizer.
    /// </summary>
    public class CluAdaptiveRecognizer : Recognizer
    {
        /// <summary>
        /// The Kind value for this recognizer.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "NegativeEddy.CluAdaptiveRecognizer";

        /// <summary>
        /// Initializes a new instance of the <see cref="CluAdaptiveRecognizer"/> class.
        /// </summary>
        public CluAdaptiveRecognizer()
        {
        }

        /// <summary>
        /// Gets or sets LUIS application ID.
        /// </summary>
        /// <value>Application ID.</value>
        [JsonProperty("projectName")]
        public StringExpression ProjectName { get; set; }

        /// <summary>
        /// Gets or sets LUIS version.
        /// </summary>
        /// <value>application version.</value>
        [JsonProperty("version")]
        public StringExpression Version { get; set; }

        /// <summary>
        /// Gets or sets LUIS endpoint like https://westus.api.cognitive.microsoft.com to query.
        /// </summary>
        /// <value>LUIS Endpoint.</value>
        [JsonProperty("endpoint")]
        public StringExpression Endpoint { get; set; }

        [JsonProperty("deploymentName")]
        public StringExpression DeploymentName { get; set; }

        /// <summary>
        /// Gets or sets the key used to talk to a LUIS endpoint.
        /// </summary>
        /// <value>Endpoint key.</value>
        [JsonProperty("endpointKey")]
        public StringExpression EndpointKey { get; set; }

        [JsonProperty("projectType")]
        public StringExpression ProjectType { get; set; } = CluProjectSettings.ProjectType_CLU;

        /// <summary>
        /// Gets or sets LUIS Prediction options (with expressions).
        /// </summary>
        /// <value>Predictions options (Declarative with expression support).</value>
        [JsonProperty("predictionOptions")]
        public CluRecognizerOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the flag to determine if personal information should be logged in telemetry.
        /// </summary>
        /// <value>
        /// The flag to indicate in personal information should be logged in telemetry.
        /// </value>
        [JsonProperty("logPersonalInformation")]
        public BoolExpression LogPersonalInformation { get; set; } = "=settings.runtimeSettings.telemetry.logPersonalInformation";

        /// <inheritdoc/>
        public override async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken = default, Dictionary<string, string> telemetryProperties = null, Dictionary<string, double> telemetryMetrics = null)
        {
            var project = new CluProjectSettings(
                ProjectName.GetValue(dialogContext.State),
                DeploymentName.GetValue(dialogContext.State),
                EndpointKey.GetValue(dialogContext.State),
                Endpoint.GetValue(dialogContext.State),
                ProjectType.GetValue(dialogContext.State));

            var recognizerOptions = new CluRecognizerOptions()
            {
                TelemetryClient = TelemetryClient,
            };

            var recognizer = new CluRecognizer(recognizerOptions, project);

            RecognizerResult result = await recognizer.RecognizeAsync(dialogContext, activity, cancellationToken).ConfigureAwait(false);

            TrackRecognizerResult(dialogContext, "CLUResult", FillRecognizerResultTelemetryProperties(result, telemetryProperties, dialogContext), telemetryMetrics);

            return result;
        }

        /// <summary>
        /// Uses the <see cref="RecognizerResult"/> returned from the <see cref="LuisRecognizer"/> and populates a dictionary of string
        /// with properties to be logged into telemetry.  Including any additional properties that were passed into the method.
        /// </summary>
        /// <param name="recognizerResult">An instance of <see cref="RecognizerResult"/> to extract the telemetry properties from.</param>
        /// <param name="telemetryProperties">A collection of additional properties to be added to the returned dictionary of properties.</param>
        /// <param name="dc">An instance of <see cref="DialogContext"/>.</param>
        /// <returns>The dictionary of properties to be logged with telemetry for the recongizer result.</returns>
        protected override Dictionary<string, string> FillRecognizerResultTelemetryProperties(RecognizerResult recognizerResult, Dictionary<string, string> telemetryProperties, DialogContext dc)
        {
            var (logPersonalInfo, error) = LogPersonalInformation.TryGetValue(dc.State);
            var (projectName, error2) = ProjectName.TryGetValue(dc.State);

            var topTwoIntents = recognizerResult.Intents.Count > 0 ? recognizerResult.Intents.OrderByDescending(x => x.Value.Score).Take(2).ToArray() : null;

            // Add the intent score and conversation id properties
            var properties = new Dictionary<string, string>()
            {
                { LuisTelemetryConstants.ApplicationIdProperty, projectName },
                { LuisTelemetryConstants.IntentProperty, topTwoIntents?[0].Key ?? string.Empty },
                { LuisTelemetryConstants.IntentScoreProperty, topTwoIntents?[0].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" },
                { LuisTelemetryConstants.Intent2Property, topTwoIntents?.Length > 1 ? topTwoIntents?[1].Key ?? string.Empty : string.Empty },
                { LuisTelemetryConstants.IntentScore2Property, topTwoIntents?.Length > 1 ? topTwoIntents?[1].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" : "0.00" },
                { LuisTelemetryConstants.FromIdProperty, dc.Context.Activity.From.Id },
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
            if (logPersonalInfo && !string.IsNullOrEmpty(dc.Context.Activity.Text))
            {
                properties.Add(LuisTelemetryConstants.QuestionProperty, dc.Context.Activity.Text);
            }

            // Additional Properties can override "stock" properties.
            if (telemetryProperties != null)
            {
                return telemetryProperties.Concat(properties)
                           .GroupBy(kv => kv.Key)
                           .ToDictionary(g => g.Key, g => g.First().Value);
            }

            return properties;
        }
    }
}
