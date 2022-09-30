namespace CustomQuestionAnsweringBotSample.LanguageUnderstanding
{
    // Classes for serializing the body of the CLU Http request
    // defined at https://learn.microsoft.com/en-us/rest/api/language/conversation-analysis-runtime/analyze-conversation

    public class ConversationalTask
    {
        public string kind { get; set; }
        public ConversationAnalysisOptions analysisInput { get; set; }
        public ConversationTaskParameters parameters { get; set; }
    }

    public class ConversationAnalysisOptions
    {
        public ConversationItemBase conversationItem { get; set; }
    }

    public class ConversationItemBase
    {
        public string id { get; set; }
        public string participantId { get; set; }
        public string text { get; set; }
        public string language { get; set; }
    }

    public class ConversationTaskParameters
    {
        public string projectName { get; set; }
        public string deploymentName { get; set; }
        public string stringIndexType { get; set; }
        public string directTarget { get; set; }
        public bool isLoggingEnabled { get; set; }
        public bool verbose { get; set; }
    }

}
