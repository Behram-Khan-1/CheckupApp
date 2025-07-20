using System;
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

    // Reference to GoalReminderManager for handling goal transitions
    private GoalReminderManager goalReminderManager;
    
    // Flag to track if we're waiting for a response about moving goals
    private bool waitingForMoveGoalsResponse = false;
    
    // Flag to indicate we're setting goals for tomorrow
    private bool _setGoalsForTomorrow = false;
    public bool SetGoalsForTomorrow
    {
        get { return _setGoalsForTomorrow; }
        set { _setGoalsForTomorrow = value; }
    }
    
    void Awake()
    {
        // Find the GoalReminderManager in the scene
        goalReminderManager = FindObjectOfType<GoalReminderManager>();
        if (goalReminderManager == null)
        {
            Debug.LogError("ChatInputManager requires a GoalReminderManager in the scene");
        }
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
        
        // Add user message to chat
        if (!string.IsNullOrEmpty(userMessage) && !string.IsNullOrWhiteSpace(userMessage))
        {
            chatUIManager.AddUserMessage(userMessage);
            inputField.text = "";
            
            // Check if we're waiting for a response about moving goals
            if (waitingForMoveGoalsResponse)
            {
                // Handle the response about moving goals
                goalReminderManager.HandleMoveGoalsResponse(userMessage);
                waitingForMoveGoalsResponse = false;
                return;
            }
            // Check if we're setting goals for tomorrow
            else if (SetGoalsForTomorrow)
            {
                // Process goals for tomorrow
                ProcessGoalsForTomorrow(userMessage);
                SetGoalsForTomorrow = false;
                return;
            }
            // Check if we're in AwaitingGoalTiming mode
            else if (chatStateController.GetChatMode() == ChatMode.AwaitingGoalTiming)
            {
                // We're waiting for timing information for a goal
                goalTimingManager.SetGoalsTiming(userMessage);
            }
            // Check if this is likely a greeting message to avoid misclassification
            else if (IsGreeting(userMessage))
            {
                // Skip intent classification for obvious greetings
                ProcessNormalConversation(userMessage);
            }
            else
            {
                // Normal flow - classify the intent of the message
                ClassifyMessageIntent(userMessage);
            }
        }
    }
    
    // Helper method to identify common greeting patterns
    private bool IsGreeting(string message)
    {
        string lowerMessage = message.ToLower().Trim();
        
        // Check for common greeting patterns
        return lowerMessage == "hi" || 
               lowerMessage == "hello" || 
               lowerMessage == "hey" || 
               lowerMessage == "hi there" ||
               lowerMessage == "hello there" ||
               lowerMessage == "good morning" ||
               lowerMessage == "good afternoon" ||
               lowerMessage == "good evening" ||
               lowerMessage == "greetings";
    }
    
    private void ClassifyMessageIntent(string message)
    {
        // Set prompt type to intent classification
        promptBuilder.SetPromptType(PromptType.IntentClassification);
        
        // Append user message to the prompt
        promptBuilder.Append(message);
        
        // Set chat state to processing
        chatStateController.SetChatState(ChatState.MessageSent);
        
        // Get the API client to process the intent classification
        GeminiApiClient apiClient = FindObjectOfType<GeminiApiClient>();
        if (apiClient != null)
        {
            apiClient.SendPrompt(promptBuilder.BuildPrompt(), OnIntentClassificationResponse);
        }
        else
        {
            Debug.LogError("GeminiApiClient not found");
            chatUIManager.AddAppMessage("Sorry, I couldn't process your message.");
        }
    }
    
    private void OnIntentClassificationResponse(string jsonResponse)
    {
        try
        {
            // Parse the JSON response to determine intent
            IntentClassification intent = JsonUtility.FromJson<IntentClassification>(jsonResponse);
            
            if (intent == null)
            {
                Debug.LogError("Failed to parse intent classification response");
                chatUIManager.AddAppMessage("Sorry, I couldn't understand your message.");
                return;
            }
            
            // Process based on intent
            switch (intent.intent)
            {
                case "goal":
                    ProcessGoalIntent(intent);
                    break;
                    
                case "streak_update":
                    ProcessStreakUpdateIntent(intent);
                    break;
                    
                case "greeting":
                case "smalltalk":
                case "other":
                    // Just reply normally
                    ProcessNormalConversation(userMessage);
                    break;
                    
                default:
                    ProcessNormalConversation(userMessage);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing intent classification: {e.Message}");
            chatUIManager.AddAppMessage("Sorry, I couldn't process your message.");
        }
    }
    
    private void ProcessGoalIntent(IntentClassification intent)
    {
        if (intent.goal != null)
        {
            string goalText = intent.goal.text;
            string timing = intent.goal.timing;
            
            if (string.IsNullOrEmpty(timing))
            {
                // No timing provided, ask for timing
                chatStateController.SetChatMode(ChatMode.AwaitingGoalTiming);
                promptBuilder.SetPromptType(PromptType.AskForTiming);
                promptBuilder.Append(goalText);
                
                // Store the goal text for later
                goalTimingManager.SetPendingGoalText(goalText);
                
                // Send the prompt to get timing
                GeminiApiClient apiClient = FindObjectOfType<GeminiApiClient>();
                if (apiClient != null)
                {
                    apiClient.SendPrompt(promptBuilder.BuildPrompt());
                }
            }
            else
            {
                // Timing provided, save the goal directly
                Goal newGoal = new Goal
                {
                    text = goalText,
                    timing = timing,
                    time = "",  // Will be calculated later
                    completed = false
                };
                
                GoalList goalList = new GoalList
                {
                    goals = new List<Goal> { newGoal }
                };
                
                // Save to database
                DatabaseManager.Instance.SaveGoalsToFirebase(goalList);
                
                // Confirm to user
                chatUIManager.AddAppMessage($"Added goal: {goalText} at {timing}");
                
                // Refresh goals display
                Invoke("RefreshGoalsDisplay", 1f);
            }
        }
    }
    
    private void ProcessStreakUpdateIntent(IntentClassification intent)
    {
        if (intent.streak != null)
        {
            string streakName = intent.streak.name;
            string streakStatus = intent.streak.status;
            
            // Create streak object
            Streak streak = new Streak
            {
                name = streakName,
                status = streakStatus
            };
            
            // Save to database
            DatabaseManager.Instance.SaveStreakUpdate(streak);
            
            // Confirm to user
            chatUIManager.AddAppMessage($"Updated streak: {streakName} - {streakStatus}");
        }
    }
    
    private void ProcessNormalConversation(string message)
    {
        // Reset to normal conversation mode
        chatStateController.SetChatMode(ChatMode.Normal);
        promptBuilder.SetPromptType(PromptType.Idle);
        
        // Add specific instructions for greeting responses to keep them short
        if (message.ToLower().Contains("hello") || message.ToLower().Contains("hi") || 
            message.ToLower().Contains("hey") || message.ToLower().Contains("morning") ||
            message.ToLower().Contains("afternoon") || message.ToLower().Contains("evening"))
        {
            promptBuilder.Append("Keep your response very brief. " + message);
        }
        else
        {
            promptBuilder.Append(message);
        }
        
        // Send the prompt for a normal reply
        GeminiApiClient apiClient = FindObjectOfType<GeminiApiClient>();
        if (apiClient != null)
        {
            apiClient.SendPrompt(promptBuilder.BuildPrompt());
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
        
        // Command to set goals for tomorrow
        if (lowerMessage == "/tomorrow" || lowerMessage == "set goals for tomorrow")
        {
            chatUIManager.AddUserMessage(message);
            chatUIManager.AddAppMessage("What goals would you like to set for tomorrow?");
            // Set a flag to indicate we're setting goals for tomorrow
            SetGoalsForTomorrow = true;
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
    
    // Process goals for tomorrow
    private void ProcessGoalsForTomorrow(string message)
    {
        // Set the prompt type to get goals
        promptBuilder.SetPromptType(PromptType.GetGoals);
        promptBuilder.Append(message);
        
        // Send the prompt to get goals
        GeminiApiClient apiClient = FindObjectOfType<GeminiApiClient>();
        if (apiClient != null)
        {
            apiClient.SendPrompt(promptBuilder.BuildPrompt(), OnTomorrowGoalsResponse);
        }
    }
    
    // Handle the response for tomorrow's goals
    private void OnTomorrowGoalsResponse(string response)
    {
        try
        {
            // Parse the goals from the response
            GoalList goalList = JsonUtility.FromJson<GoalList>(response);
            
            if (goalList != null && goalList.goals != null && goalList.goals.Count > 0)
            {
                // Calculate tomorrow's date
                string tomorrowDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                
                // Save the goals for tomorrow's date
                DatabaseManager.Instance.SaveGoalsForDate(goalList, tomorrowDate);
                
                // Confirm to the user
                chatUIManager.AddAppMessage("I've set your goals for tomorrow. You can view them tomorrow or by using the /goals command.");
            }
            else
            {
                chatUIManager.AddAppMessage("I couldn't understand your goals. Please try again with clearer goals.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error processing tomorrow's goals: " + ex.Message);
            chatUIManager.AddAppMessage("There was an error setting your goals for tomorrow. Please try again.");
        }
    }
    
    private void ShowHelpCommands()
    {
        string helpMessage = "Available Commands:\n\n" +
                           "• /goals or 'show goals' - Display all your goals\n" +
                           "• /complete [goal text] - Mark a goal as completed\n" +
                           "• /test [goal text] - Test a reminder for a specific goal\n" +
                           "• /tomorrow - Set goals for tomorrow\n" +
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

