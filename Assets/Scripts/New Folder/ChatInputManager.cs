using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ChatInputManager : MonoBehaviour
{
    PromptBuilder promptBuilder;
    string userMessage;
    public TMP_InputField inputField; // User input field
    public ChatUIManager chatUIManager; // assign in Inspector
    public ChatStateController chatStateController;
    public GoalTimingManager goalTimingManager;


    void Start()
    {
        inputField.onValueChanged.AddListener(OnValueChanged);
        promptBuilder = PromptService.Instance.promptBuilder;
        

    }

    public void OnSendButtonClicked()
    {
        userMessage = inputField.text;
        
        // Check for special commands
        if (ProcessSpecialCommands(userMessage))
        {
            inputField.text = "";
            return;
        }
        
        // Normal message processing
        promptBuilder.Append(userMessage);
        if (!string.IsNullOrEmpty(userMessage) && !string.IsNullOrWhiteSpace(userMessage))
        {
            chatUIManager.AddUserMessage(userMessage);
            inputField.text = "";
            chatStateController.SetChatState(ChatState.MessageSent);

            if (chatStateController.GetChatMode() == ChatMode.AwaitingGoalTiming)
            {
                goalTimingManager.SetGoalsTiming(userMessage);
            }
        }
    }
    
    private bool ProcessSpecialCommands(string message)
    {
        string lowerMessage = message.ToLower().Trim();
        
        // Help command
        if (lowerMessage == "/help" || lowerMessage == "help" || lowerMessage == "commands")
        {
            chatUIManager.AddUserMessage(message);
            ShowHelpCommands();
            return true;
        }
        
        // Command to show goals
        if (lowerMessage == "/goals" || lowerMessage == "show goals" || lowerMessage == "my goals")
        {
            chatUIManager.AddUserMessage(message);
            GoalReminderManager reminderManager = FindObjectOfType<GoalReminderManager>();
            if (reminderManager != null)
            {
                reminderManager.ShowTodaysGoals();
            }
            else
            {
                chatUIManager.AddAppMessage("Goal reminder system is not available.");
            }
            return true;
        }
        
        // Command to mark a goal as complete
        if (lowerMessage.StartsWith("/complete ") || lowerMessage.StartsWith("complete "))
        {
            string goalText = message.Substring(message.IndexOf(' ') + 1).Trim();
            if (!string.IsNullOrEmpty(goalText))
            {
                chatUIManager.AddUserMessage(message);
                DatabaseManager.Instance.MarkGoalAsCompleted(goalText, true);
                chatUIManager.AddAppMessage($"✅ Marked goal as completed: {goalText}");
                
                // Refresh goals display
                Invoke("RefreshGoalsDisplay", 1f);
            }
            return true;
        }
        
        // Command to test a reminder for a goal
        if (lowerMessage.StartsWith("/test ") || lowerMessage.StartsWith("test reminder "))
        {
            string goalText = message.Substring(message.IndexOf(' ') + 1).Trim();
            if (!string.IsNullOrEmpty(goalText))
            {
                chatUIManager.AddUserMessage(message);
                GoalReminderManager reminderManager = FindObjectOfType<GoalReminderManager>();
                if (reminderManager != null)
                {
                    chatUIManager.AddAppMessage($"⏱️ Testing reminder for: {goalText} (will appear in 3 seconds)");
                    reminderManager.TestReminderForGoal(goalText);
                }
                else
                {
                    chatUIManager.AddAppMessage("Goal reminder system is not available.");
                }
            }
            return true;
        }
        
        return false;
    }
    
    private void ShowHelpCommands()
    {
        string helpMessage = "📋 Available Commands:\n\n" +
                           "• /goals or 'show goals' - Display all your goals\n" +
                           "• /complete [goal text] - Mark a goal as completed\n" +
                           "• /test [goal text] - Test a reminder for a specific goal\n" +
                           "• /help - Show this help message\n\n" +
                           "You can also just type normally to add new goals!";
        
        chatUIManager.AddAppMessage(helpMessage);
    }
    
    private void RefreshGoalsDisplay()
    {
        GoalReminderManager reminderManager = FindObjectOfType<GoalReminderManager>();
        if (reminderManager != null)
        {
            reminderManager.ShowTodaysGoals();
        }
    }

    private void OnValueChanged(string text)
    {
        if (text.Length > 0)
        {
            chatStateController.SetChatState(ChatState.Typing);
        }
        if (string.IsNullOrEmpty(text))
        {
            chatStateController.SetChatState(ChatState.Idle);
        }
    }

}

