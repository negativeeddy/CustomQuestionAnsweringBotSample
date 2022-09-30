namespace CustomQuestionAnsweringBotSample.LanguageUnderstanding
{
    // Classes for serializing the body of the CLU Http response
    // defined at https://learn.microsoft.com/en-us/rest/api/language/conversation-analysis-runtime/analyze-conversation

    public class AnalyzeConversationResult
    {
        public string kind { get; set; }
        public ConversationResult result { get; set; }
    }

    public class ConversationResult
    {
        public string query { get; set; }
        public ConversationPrediction prediction { get; set; }
        public string detectedLanguage { get; set; }
    }

    public class ConversationPrediction
    {
        public string topIntent { get; set; }
        public string projectKind { get; set; }
        public ConversationIntent[] intents { get; set; }
        public ConversationEntity[] entities { get; set; }
    }

    public class ConversationIntent
    {
        public string category { get; set; }
        public float confidenceScore { get; set; }
    }


    public class ConversationEntity
    {
        public string category { get; set; }
        public string text { get; set; }
        public int offset { get; set; }
        public int length { get; set; }
        public int confidenceScore { get; set; }
    }

}