[System.Serializable]
public class Part
{
    public string text;
}

[System.Serializable]
public class Content
{
    public Part[] parts;
}

[System.Serializable]
public class GeminiRequest
{
    public Content[] contents;
}

//Response from GEmini
[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}
