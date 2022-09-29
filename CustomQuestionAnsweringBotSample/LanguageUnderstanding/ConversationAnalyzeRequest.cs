namespace CustomQuestionAnsweringBotSample.LanguageUnderstanding
{
    public class ConversationAnalyzeRequest
    {
        public string kind { get; set; }
        public Analysisinput analysisInput { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Analysisinput
    {
        public Conversationitem conversationItem { get; set; }
    }

    public class Conversationitem
    {
        public string id { get; set; }
        public string participantId { get; set; }
        public string text { get; set; }
        public string language { get; set; }
    }

    public class Parameters
    {
        public string projectName { get; set; }
        public string deploymentName { get; set; }
        public string stringIndexType { get; set; }
        public string directTarget { get; set; }
        public bool isLoggingEnabled { get; set; }
        public bool verbose { get; set; }
    }

}
