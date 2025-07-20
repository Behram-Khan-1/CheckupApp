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
                return askTimingPrompt + " \n " + fullPrompt;
            case PromptType.SetGoalTiming:
                return timingPrompt + " \n " + fullPrompt;
            case PromptType.AskForCompletion:
                return "The user had the following task: '" + fullPrompt + "'. Ask if they completed it today.";
            case PromptType.MotivationReply:
                return "Send a short motivational message related to: " + fullPrompt;
            case PromptType.IntentClassification:
                return intentClassificationPrompt + " \n " + fullPrompt;
            default:
                return "Be a supportive assistant. Keep your responses brief and concise, especially for greetings and casual conversation. Do not use emojis in any responses.";
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


    private string goalPrompt = @"You are a goal-tracking assistant.
  The user has entered a series of messages describing what they want to achieve today or in general.
  Your job is to extract clear, structured goals from these messages.
  Respond ONLY in this JSON format:
  
  Respond ONLY in this JSON format:
{
  ""goals"": [
    {
      ""text"": ""Detailed goal 1"",
      ""timing"": ""Timing""
    },
    {
      ""text"": ""Detailed goal 2"",
      ""timing"": ""Timing""
    }
  ]
}

  Example 1:
  goals: [
    {{
      text: Workout for 30 minute at 7pm
      timing: 7-7:30 pm,

      text: wake up at 9am
      timing: 9am
    }},
  ]

  Example 2:
  goals: [
    {{
      text: Sleep at 8pm and wakeup at 6am
      timing: 8pm - 6am,

      text: meditation
      timing: 7am,

      text: work
      timing: 9am-1pm
    }},
  ]
  Rules:
  - Only include goals that are actionable or trackable.
  - If the user specifies timing (like 'by next week', 'in 2 days', 'this month', 'at 8am'), extract it under timing
  - If no timing is mentioned, leave timing empty.
   Below is the users message ";


    private string askTimingPrompt = @"
You are a helpful and supportive goal-tracking assistant.
The user has already entered some goals, but did not specify when they would like to work on or complete some of them.
Your job is to ask the user for the timing or deadline of each goal.

Guidelines:
- If a goal is a task/project, ask when they would like to complete it (today, in 3 days, by next month, at 8pm, etc.)
- Do not include goals that the user didn’t mention.

Goals:
";

    private string timingPrompt = @"
You are a helpful and supportive goal-tracking assistant.
The user has entered timing of the goals he didnt mention earlier, 
You have to map each goal to its timing now and then output their responses in the following JSON format:
Respond ONLY in this JSON format:
{
  ""goals"": [
    {
      ""text"": ""Workout"",
      ""timing"": ""Every morning""
    },
    {
      ""text"": ""Apply to a job"",
      ""timing"": ""By the end of this week""
    }
  ]
}

  Example 1:
  goals: [
    {{
      text: Workout for 30 minute at 7pm
      timing: 7-7:30 pm,

      text: wake up at 9am
      timing: 9am
    }},
  ]

  Example 2:
  goals: [
    {{
      text: Sleep at 8pm and wakeup at 6am
      timing: 8pm - 6am,

      text: meditation
      timing: 7am,

      text: work
      timing: 9am-1pm
    }},
  ]
  
Guidelines:
- If a goal is a task/project, ask when they would like to complete it (today, in 3 days, by next month, at 8pm, etc.)
- Ensure that the text field exactly matches the goal's original description.
- Do not include goals that the user didn’t mention.

Goals:
";

    private string intentClassificationPrompt = @"You are a goal-tracking assistant that can classify user messages by intent.
Analyze the user's message and determine the primary intent.

Respond ONLY in this JSON format:
{
  ""intent"": ""[intent_type]"",
  ""details"": {}
}

The intent_type must be one of:
- ""goal:"": User is setting a goal or task to complete (ONLY classify as goal if the user is clearly stating something they want to accomplish or do)
- ""streak_update"": User is updating a streak (like NoFap, workout, meditation)
- ""greeting"": User is saying hello or starting a conversation (e.g., hi, hello, hey, good morning)
- ""smalltalk"": User is making casual conversation (e.g., how are you, what's up)
- ""other"": Any other intent not covered above

For each intent type, include these specific details:

1. If intent is ""goal"":
{
  ""intent"": ""goal"",
  ""goal"": {
    ""text"": ""[full goal description]"",
    ""timing"": ""[timing if specified, otherwise empty]""
  }
}

2. If intent is ""streak_update"":
{
  ""intent"": ""streak_update"",
  ""streak"": {
    ""name"": ""[streak name, e.g. 'nofap', 'workout', 'meditation']"",
    ""status"": ""[status update, e.g. 'completed', 'failed', 'day 5']""
  }
}

3. For other intents (greeting, smalltalk, other):
{
  ""intent"": ""[intent_type]""
}

Examples:
- ""I need to finish my project by tomorrow"" → {""intent"": ""goal"", ""goal"": {""text"": ""finish my project"", ""timing"": ""tomorrow""}}
- ""I want to start exercising"" → {""intent"": ""goal"", ""goal"": {""text"": ""start exercising"", ""timing"": ""}
- ""I worked out today"" → {""intent"": ""streak_update"", ""streak"": {""name"": ""workout"", ""status"": ""completed""}}
- ""Hello"" → {""intent"": ""greeting""}
- ""Hi there"" → {""intent"": ""greeting""}
- ""Good morning"" → {""intent"": ""greeting""}
- ""How are you?"" → {""intent"": ""smalltalk""}
- ""What's the weather like?"" → {""intent"": ""smalltalk""}

Analyze the following user message:";
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