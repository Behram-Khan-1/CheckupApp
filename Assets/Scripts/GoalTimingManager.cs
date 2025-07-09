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
        GoalsNeedingTiming();
    }

}