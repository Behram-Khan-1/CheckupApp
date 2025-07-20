using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoalTimingManager
{
    public ChatStateController chatStateController;
    public ChatUIManager chatUIManager;
    private GeminiApiClient geminiApiClient;
    private PromptBuilder promptBuilder;
    private string pendingGoalText; // Store the goal text that is waiting for timing
    public GoalTimingManager(ChatStateController chatStateController,
     GeminiApiClient geminiApiClient,
     ChatUIManager chatUIManager)
    {
        this.chatStateController = chatStateController;
        this.chatUIManager = chatUIManager;
        this.geminiApiClient = geminiApiClient;

        promptBuilder = PromptService.Instance.promptBuilder;
    }
    public void GoalsNeedingTiming()
    {
        DatabaseManager.Instance.LoadGoalsFromFirebase(goalList =>
        {
            List<Goal> goalsNeedingTiming = goalList.goals.Where(g => string.IsNullOrEmpty(g.timing)).ToList();

            if (goalsNeedingTiming.Count <= 0)
            {
                Debug.Log("No goals need timing ");
                return;
            }
            promptBuilder.SetPromptType(PromptType.AskForTiming);
            chatStateController.SetChatMode(ChatMode.AwaitingGoalTiming);
            promptBuilder.Reset();
            foreach (Goal goal in goalsNeedingTiming)
            {
                promptBuilder.Append(goal.text);
            }
            Debug.Log("GoalNeedTiming" + promptBuilder.GetPrompt());
            geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(), chatUIManager.AddAppMessage);
        });

    }

    public void SetGoalsTiming(string userMessage)
    {
        promptBuilder.SetPromptType(PromptType.SetGoalTiming);

        // If we have a pending goal text, we should use it directly
        if (!string.IsNullOrEmpty(pendingGoalText))
        {
            promptBuilder.Reset();
            promptBuilder.Append(pendingGoalText);
            promptBuilder.Append("User Timing Response " + userMessage);
            
            Debug.Log("SetGoalTiming for pending goal");
            geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(),
             chatUIManager.AddAppMessage,
            UpdateGoaltiming);

            // Clear the pending goal text after processing
            pendingGoalText = null;
            
            chatStateController.SetChatMode(ChatMode.Normal);
            chatStateController.SetChatState(ChatState.Idle);
            return;
        }
        
        // Otherwise, load goals from Firebase as before
        DatabaseManager.Instance.LoadGoalsFromFirebase(goalList =>
        {
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
            geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(),
             chatUIManager.AddAppMessage,
            UpdateGoaltiming);

            chatStateController.SetChatMode(ChatMode.Normal);
            chatStateController.SetChatState(ChatState.Idle);
        });
    }

    public void UpdateGoaltiming(string jsonResponse)
    {
        var response = JsonUtility.FromJson<GoalList>(jsonResponse);
        DatabaseManager.Instance.UpdateGoalsTiming(response.goals);
        
        // Clear any remaining pendingGoalText
        pendingGoalText = null;
        
        GoalsNeedingTiming();
    }

    // Store the goal text that is waiting for timing information
    public void SetPendingGoalText(string goalText)
    {
        pendingGoalText = goalText;
    }

    // Get the pending goal text
    public string GetPendingGoalText()
    {
        return pendingGoalText;
    }
}