using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Azure.AI.Language.Conversations;
using Microsoft.Bot.Builder;
using static System.Net.Mime.MediaTypeNames;

namespace CustomQuestionAnsweringBotSample.LanguageUnderstanding
{
    public class CluRecognizerOptions
    {
        /// <summary>
        /// The version of the api to use.
        /// </summary>
        //public ConversationAnalysisOptions.ServiceVersion ApiVersion = ConversationAnalysisClientOptions.ServiceVersion.V2021_11_01_Preview;

        /// <summary>
        /// Creates an instance of  <see cref="CluRecognizerOptions"/> containing the CLU Application as well as optional configurations.
        /// </summary>
        public CluRecognizerOptions(CluApplication application)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
        }

        /// <summary>
        /// An instance of the <see cref="Application"/> class containing connection details for your CLU application.
        /// </summary>
        public CluApplication Application { get; }

        /// <summary>
        /// If true, the query will be kept by the service for customers to further review, to improve the model quality.
        /// </summary>
        public bool? IsLoggingEnabled { get; set; }

        /// <summary>
        /// The language to be used with this recognizer.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// If set to true, the service will return a more verbose response.
        /// </summary>
        public bool? Verbose { get; set; }

        public ConversationsClientOptions.ServiceVersion? ApiVersion { get; set; }

        /// <summary>
        /// The name of the target project this request is sending to directly.
        /// </summary>
        public string DirectTarget { get; set; }

        /// <summary>
        /// A dictionary representing the input for each target project.
        /// </summary>
        //public IDictionary<string, AnalysisParameters> Parameters { get; }

        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();
        public bool LogPersonalInformation { get; set; } = false;
        public double Timeout { get; set; } = 100000;

        public bool IncludeAPIResults { get; set; }

    }
}