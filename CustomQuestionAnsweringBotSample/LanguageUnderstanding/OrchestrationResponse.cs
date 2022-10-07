namespace CustomQuestionAnsweringBotSample.LanguageUnderstanding
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
        public Intents intents { get; set; }
    }

    public class Intents
    {
        public LanguageIntent LanguageIntent { get; set; }
        public QnaIntent QnAIntent { get; set; }
        public None None { get; set; }
    }

    public class LanguageIntent
    {
        public float confidenceScore { get; set; }
        public string targetProjectKind { get; set; }
        public LanguageResult result { get; set; }
    }

    public class LanguageResult
    {
        public string query { get; set; }
        public ConversationPrediction prediction { get; set; }
    }


    public class QnaIntent
    {
        public float confidenceScore { get; set; }
        public string targetProjectKind { get; set; }
        public QnaResult result { get; set; }
    }

    public class QnaResult
    {
        public QnaAnswer[] answers { get; set; }
    }

    public class QnaAnswer
    {
        public string[] questions { get; set; }
        public string answer { get; set; }
        public float confidenceScore { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public Metadata metadata { get; set; }
        public Dialog dialog { get; set; }
        public Dialog context => dialog;    // return a context object for existing Composer logic
    }

    public class Metadata
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


    public class None
    {
        public float confidenceScore { get; set; }
        public string targetProjectKind { get; set; }
    }
}