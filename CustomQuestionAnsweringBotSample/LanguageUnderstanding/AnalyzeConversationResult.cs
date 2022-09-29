public class AnalyzeConversationResult
{
    public string kind { get; set; }
    public CluResult result { get; set; }
}

public class CluResult
{
    public string query { get; set; }
    public ConversationPrediction prediction { get; set; }
    public string detectedLanguage { get; set; }
}

public class ConversationPrediction
{
    public string topIntent { get; set; }
    public string projectKind { get; set; }
    public Intent[] intents { get; set; }
    public CluEntity[] entities { get; set; }
}

public class Intents
{
    public Rail Rail { get; set; }
    public Tree Tree { get; set; }
    public None None { get; set; }
}

public class Rail
{
    public int confidenceScore { get; set; }
    public string targetProjectKind { get; set; }
    public Result1 result { get; set; }
}

public class Result1
{
    public string query { get; set; }
    public Prediction1 prediction { get; set; }
}

public class Prediction1
{
    public string topIntent { get; set; }
    public string projectKind { get; set; }
    public Intent[] intents { get; set; }
    public object[] entities { get; set; }
}

public class Intent
{
    public string category { get; set; }
    public float confidenceScore { get; set; }
}

public class Tree
{
    public float confidenceScore { get; set; }
    public string targetProjectKind { get; set; }
}

public class None
{
    public int confidenceScore { get; set; }
    public string targetProjectKind { get; set; }
}


public class CluEntity
{
    public string category { get; set; }
    public string text { get; set; }
    public int offset { get; set; }
    public int length { get; set; }
    public int confidenceScore { get; set; }
}

