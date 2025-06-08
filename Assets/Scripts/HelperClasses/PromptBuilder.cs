using System;
using System.Diagnostics;
public class PromptBuilder
{
    private string fullPrompt = "";

    public void Append(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(fullPrompt))
            fullPrompt = userMessage;
        else
            fullPrompt += "\n" + userMessage;
    }

    public string GetPrompt() => fullPrompt;

    public void Reset() => fullPrompt = "";


    private string goalPrompt =  @$"You are a goal-tracking assistant.
The user has entered a series of messages describing what they want to achieve today or in general. Your job is to extract clear, structured goals from these messages.
Respond ONLY in this JSON format:

  ""goals"": [
    {{
      ""text"": ""Workout for 30 minutes"",
      ""type"": ""habit/daily/one-time"",
      ""priority"": ""high/medium/low"",
      ""streak"": true/false
    }},
  ]


Rules:
- Only include goals that are actionable or trackable.
- Classify the goal type:
  - 'habit' for recurring behaviors like ""NoFap"" or ""Avoid sugar""
  - 'daily' for tasks to complete today like ""Apply to job""
  - 'one-time' for something like ""Buy running shoes""
- Set streak = true if it's a habit to be tracked over days.
- Try your best to infer priority based on language. Otherwise default to ""medium"" ";


    public string BuildGoalExtractionPrompt()
    {
        return goalPrompt + fullPrompt;
    }
}

