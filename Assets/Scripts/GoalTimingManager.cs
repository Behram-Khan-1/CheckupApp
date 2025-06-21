using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoalTimingManager
{
    private ChatManager chatManager;
    private GeminiApiClient geminiApiClient;
    private PromptBuilder promptBuilder = new PromptBuilder();
    public GoalTimingManager(ChatManager chatManager, GeminiApiClient geminiApiClient)
    {
        this.chatManager = chatManager;
        this.geminiApiClient = geminiApiClient;
    }

    public void GoalsNeedingTiming()
    {

        GoalList goalList = JsonTaskStorage.LoadTasks();
        List<Goal> goalsNeedingTiming = goalList.goals.Where(g => string.IsNullOrEmpty(g.timing)).ToList();

        if (goalsNeedingTiming.Count <= 0)
        {
            Debug.Log("No goals need timing ");
            return;
        }
        promptBuilder.SetPromptType(PromptType.AskForTiming);
        chatManager.SetChatMode(ChatMode.AwaitingGoalTiming);
        promptBuilder.Reset();
        foreach (Goal goal in goalsNeedingTiming)
        {
            promptBuilder.Append(goal.text);
        }
        Debug.Log("GoalNeedTiming" + promptBuilder.GetPrompt());
        geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(), chatManager.AddAppMessage);

    }

    public void SetGoalsTiming(string userMessage)
    {
        promptBuilder.SetPromptType(PromptType.SetGoalTiming);

        GoalList goalList = JsonTaskStorage.LoadTasks();
        List<Goal> goalsNeedingTiming = goalList.goals.Where(g => string.IsNullOrEmpty(g.timing)).ToList();

        if (goalsNeedingTiming.Count <= 0)
        {
            Debug.Log("No goals need timing ");
            return;
        }
        promptBuilder.Reset();

        foreach (Goal goal in goalsNeedingTiming)
        {
            promptBuilder.Append(goal.text);
        }
        promptBuilder.Append("User Timing Response " + userMessage);

        Debug.Log("SetGoalTiming");
        geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(), chatManager.AddAppMessage, chatManager.UpdateGoaltiming);
        chatManager.SetChatMode(ChatMode.Normal);    
        chatManager.SetChatState(ChatState.Idle);
    }
}