using System;
using System.Diagnostics;
public class PromptBuilder
{

  PromptType currentPrompt = PromptType.GetGoals;

  public void SetPromptType(PromptType type)
  {
    currentPrompt = type;
  }
  public string BuildPrompt()
  {
    switch (currentPrompt)
    {
      case PromptType.GetGoals:
        return goalPrompt + " \n " + fullPrompt;
      case PromptType.AskForTiming:
        return "Based on the following tasks, ask the user when they would like to be reminded or checked in:\n" + fullPrompt;
      case PromptType.AskForCompletion:
        return "The user had the following task: '" + fullPrompt + "'. Ask if they completed it today.";
      case PromptType.MotivationReply:
        return "Send a short motivational message related to: " + fullPrompt;
      default:
        return "Be a supportive assistant.";
    }
  }




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


  private string goalPrompt = @$"You are a goal-tracking assistant.
The user has entered a series of messages describing what they want to achieve today or in general. Your job is to extract clear, structured goals from these messages.
Respond ONLY in this JSON format:
  ""goals"": [
    {{
      ""text"": ""Workout for 30 minutes""
    }},
  ]
Rules:
- Only include goals that are actionable or trackable.
- Try your best to infer priority based on language. Otherwise default to ""medium"", Below is the users message ";


  // public string BuildGoalExtractionPrompt()
  // {
  //   return goalPrompt + fullPrompt;
  // }
}



//     private string goalPrompt =  @$"You are a goal-tracking assistant.
// The user has entered a series of messages describing what they want to achieve today or in general. Your job is to extract clear, structured goals from these messages.
// Respond ONLY in this JSON format:

//   ""goals"": [
//     {{
//       ""text"": ""Workout for 30 minutes"",
//       ""type"": ""habit/daily/one-time"",
//       ""priority"": ""high/medium/low"",
//       ""streak"": true/false
//     }},
//   ]


// Rules:
// - Only include goals that are actionable or trackable.
// - Classify the goal type:
//   - 'habit' for recurring behaviors like ""NoFap"" or ""Avoid sugar""
//   - 'daily' for tasks to complete today like ""Apply to job""
//   - 'one-time' for something like ""Buy running shoes""
// - Set streak = true if it's a habit to be tracked over days.
// - Try your best to infer priority based on language. Otherwise default to ""medium"" ";