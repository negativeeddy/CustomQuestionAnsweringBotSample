using System.Collections.Generic;

namespace AzureLanguageServiceRecognizers.LanguageUnderstanding
{

    public class OrchestrationResponse
    {
        public string kind { get; set; }
        public OrchestrationResult result { get; set; }
    }

    public class OrchestrationResult
    {
        public string query { get; set; }
        public Prediction prediction { get; set; }
        public string detectedLanguage { get; set; }
    }

    public class Prediction
    {
        public string topIntent { get; set; }
        public string projectKind { get; set; }
        public Dictionary<string, LanguageIntent> intents { get; set; }
    }

    public class LanguageIntent
    {
        public float confidenceScore { get; set; }
        public string targetProjectKind { get; set; }
        public IntentDetails result { get; set; }
    }

    public class IntentDetails
    {
        public string query { get; set; }
        public ConversationPrediction prediction { get; set; }
        public QnaAnswer[] answers { get; set; }
    }

    public class QnaAnswer
    {
        public string[] questions { get; set; }
        public string answer { get; set; }
        public float confidenceScore { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public QnaMetadata metadata { get; set; }
        public Dialog dialog { get; set; }
        public Dialog context => dialog;    // return a context object for existing Composer logic
    }

    public class QnaMetadata
    {
        public string system_metadata_qna_edited_manually { get; set; }
    }

    public class Dialog
    {
        public bool isContextOnly { get; set; }
        public Prompt[] prompts { get; set; }
    }

    public class Prompt
    {
        public int displayOrder { get; set; }
        public int qnaId { get; set; }
        public string displayText { get; set; }
    }
}