using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class GoalReminderManager : MonoBehaviour
{
    [SerializeField] private float checkInterval = 60f; // Check every minute
    private float timer;
    private DatabaseManager databaseManager;
    private ChatUIManager chatUIManager;
    
    // For day change detection
    private string currentDate;
    private bool hasCheckedYesterdaysGoals = false;
    
    void Start()
    {
        timer = checkInterval;
        databaseManager = GetComponent<DatabaseManager>();
        chatUIManager = FindObjectOfType<ChatUIManager>();
        
        if (databaseManager == null)
        {
            Debug.LogError("GoalReminderManager requires a DatabaseManager component");
        }
        
        if (chatUIManager == null)
        {
            Debug.LogError("GoalReminderManager requires a ChatUIManager in the scene");
        }
        
        // Initialize current date
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Show welcome message and today's goals after a short delay
        Invoke("ShowWelcomeMessage", 1f);
        Invoke("ShowTodaysGoals", 2f);
        
        // Check if we need to handle day transition
        CheckForDayChange();
    }
    
    private void ShowWelcomeMessage()
    {
        if (chatUIManager != null)
        {
            chatUIManager.AddAppMessage("👋 Welcome to your Goal Reminder! Type /help to see available commands.");
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            CheckGoalReminders();
            CheckForDayChange();
            timer = checkInterval;
        }
    }
    
    // Check if the day has changed and handle goal transitions
    private void CheckForDayChange()
    {
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Check if it's a new day
        if (todayDate != currentDate)
        {
            Debug.Log("Day changed from " + currentDate + " to " + todayDate);
            
            // Update the current date
            string yesterdayDate = currentDate;
            currentDate = todayDate;
            
            // Reset the flag for checking yesterday's goals
            hasCheckedYesterdaysGoals = false;
            
            // Check if it's morning (between 5 AM and 10 AM)
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 5 && currentHour <= 10 && !hasCheckedYesterdaysGoals)
            {
                // Ask about yesterday's goals during morning hours
                AskAboutYesterdaysGoals(yesterdayDate);
            }
            // If it's after 10 AM, just mark as checked without asking
            else if (currentHour > 10)
            {
                hasCheckedYesterdaysGoals = true;
            }
            
            // Show today's goals after a day change
            Invoke("ShowTodaysGoals", 2f);
        }
        // If it's a new day but we haven't checked yesterday's goals yet
        else if (!hasCheckedYesterdaysGoals)
        {
            // Calculate yesterday's date
            string yesterdayDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            
            // Check if it's morning (between 5 AM and 10 AM)
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 5 && currentHour <= 10)
            {
                // Ask about yesterday's goals during morning hours
                AskAboutYesterdaysGoals(yesterdayDate);
            }
            // If it's after 10 AM, just mark as checked without asking
            else if (currentHour > 10)
            {
                hasCheckedYesterdaysGoals = true;
            }
        }
    }
    
    // For testing purposes - trigger a reminder for a specific goal in a few seconds
    public void TestReminderForGoal(string goalText)
    {
        StartCoroutine(TestReminderCoroutine(goalText));
    }
    
    private IEnumerator TestReminderCoroutine(string goalText)
    {
        yield return new WaitForSeconds(3f);
        
        string reminderMessage = $"⏰ TEST REMINDER: It's time for your goal: {goalText}";
        Debug.Log(reminderMessage);
        
        if (chatUIManager != null)
        {
            chatUIManager.AddAppMessage(reminderMessage);
        }
        
        // Play notification sound if available
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    private void CheckGoalReminders()
    {
        DatabaseManager.Instance.LoadGoalsFromFirebase(goalList =>
        {
            DateTime currentTime = DateTime.Now;
            string today = currentTime.ToString("yyyy-MM-dd"); // Today's date
            
            foreach (Goal goal in goalList.goals)
            {
                if (string.IsNullOrEmpty(goal.timing) || goal.completed)
                    continue;
                
                // Skip if the goal has a specific date that is not today
                if (HasSpecificDate(goal.timing) && !ContainsDate(goal.timing, today)) continue;
                
                if (IsTimeToRemind(goal.timing, currentTime))
                {
                    // Send reminder to console
                    string reminderMessage = $"⏰ REMINDER: It's time for your goal: {goal.text}";
                    Debug.Log(reminderMessage);
                    
                    // Also display in chat UI
                    if (chatUIManager != null)
                    {
                        chatUIManager.AddAppMessage(reminderMessage);
                    }
                    
                    // Optionally, you could add a way for users to mark the goal as completed here
                    // For example, by adding a button to the chat UI message
                }
            }
        });
    }
    
    // Check if the timing string contains a specific date
    private bool HasSpecificDate(string timing)
    {
        // Look for patterns like YYYY-MM-DD or MM/DD/YYYY
        return System.Text.RegularExpressions.Regex.IsMatch(timing, @"\d{4}-\d{2}-\d{2}") || 
               System.Text.RegularExpressions.Regex.IsMatch(timing, @"\d{1,2}/\d{1,2}/\d{4}");
    }
    
    // Check if the timing string contains the specified date
    private bool ContainsDate(string timing, string date)
    {
        // Convert date formats if needed
        if (timing.Contains(date)) return true;
        
        // Try to parse MM/DD/YYYY format
        try
        {   
            if (System.Text.RegularExpressions.Regex.IsMatch(timing, @"\d{1,2}/\d{1,2}/\d{4}"))
            {
                string[] dateParts = System.Text.RegularExpressions.Regex.Match(timing, @"\d{1,2}/\d{1,2}/\d{4}").Value.Split('/');
                if (dateParts.Length == 3)
                {
                    string formattedDate = $"{dateParts[2]}-{dateParts[0].PadLeft(2, '0')}-{dateParts[1].PadLeft(2, '0')}";
                    return formattedDate == date;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error parsing date: {ex.Message}");
        }
        
        return false;
    }
    
    // Ask about yesterday's incomplete goals
    private void AskAboutYesterdaysGoals(string yesterdayDate)
    {
        // Mark that we've checked yesterday's goals
        hasCheckedYesterdaysGoals = true;
        
        // Load goals from yesterday
        DatabaseManager.Instance.LoadGoalsForDate(yesterdayDate, goalList =>
        {
            // Filter for incomplete goals
            List<Goal> incompleteGoals = goalList.goals.Where(g => !g.completed).ToList();
            
            if (incompleteGoals.Count == 0)
            {
                // No incomplete goals from yesterday
                string greeting2 = GetTimeBasedGreeting();
                chatUIManager.AddAppMessage($"{greeting2} You completed all your goals yesterday. 🎉 Would you like to set new goals for today?");
                return;
            }
            
            // Ask about incomplete goals
            string greeting = GetTimeBasedGreeting();
            string message = $"{greeting} You have {incompleteGoals.Count} incomplete goal{(incompleteGoals.Count > 1 ? "s" : "")} from yesterday:\n";
            
            foreach (Goal goal in incompleteGoals)
            {
                // Include timing information if available
                string timingInfo = string.IsNullOrEmpty(goal.timing) ? "" : $" ({goal.timing})";
                message += $"• {goal.text}{timingInfo}\n";
            }
            
            message += "\nWould you like to move these to today's goals? (Reply with 'yes' or 'no')";
            
            chatUIManager.AddAppMessage(message);
            
            // Store incomplete goals for later use
            _pendingIncompleteGoals = incompleteGoals;
            
            // Set the flag in ChatInputManager to wait for a response
            ChatInputManager chatInputManager = FindObjectOfType<ChatInputManager>();
            if (chatInputManager != null)
            {
                // Use reflection to set the private field
                System.Reflection.FieldInfo fieldInfo = chatInputManager.GetType().GetField("waitingForMoveGoalsResponse", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(chatInputManager, true);
                }
                else
                {
                    Debug.LogError("Could not find waitingForMoveGoalsResponse field in ChatInputManager");
                }
            }
        });
    }
    
    // Get a greeting based on the time of day
     private string GetTimeBasedGreeting()
     {
         int hour = DateTime.Now.Hour;
         
         if (hour >= 5 && hour < 12)
         {
             return "Good morning! 🌅";
         }
         else if (hour >= 12 && hour < 17)
         {
             return "Good afternoon! 🌞";
         }
         else if (hour >= 17 && hour < 22)
         {
             return "Good evening! 🌆";
         }
         else
         {
             return "Hello! 👋";
         }
     }
    
    // List of incomplete goals pending user decision
    private List<Goal> _pendingIncompleteGoals = new List<Goal>();
    
    // Method to handle user's response about moving goals
    public void HandleMoveGoalsResponse(string response)
    {
        if (_pendingIncompleteGoals == null || _pendingIncompleteGoals.Count == 0)
        {
            return; // No pending goals to move
        }
        
        if (response.ToLower().Contains("yes") || response.ToLower().Contains("yeah") || 
            response.ToLower().Contains("sure") || response.ToLower().Contains("ok"))
        {
            // Move incomplete goals to today
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            GoalList newGoalList = new GoalList { goals = new List<Goal>() };
            
            foreach (Goal goal in _pendingIncompleteGoals)
            {
                // Create a new goal for today
                Goal newGoal = new Goal
                {
                    text = goal.text,
                    timing = goal.timing,
                    time = DateTime.Now.ToString("h:mmtt dd MMMM dddd yyyy"),
                    completed = false
                };
                
                newGoalList.goals.Add(newGoal);
            }
            
            // Save the moved goals to today's date
            DatabaseManager.Instance.SaveGoalsForDate(newGoalList, today);
            
            chatUIManager.AddAppMessage("I've moved your incomplete goals to today. Good luck!");
            
            // Clear the pending goals
            _pendingIncompleteGoals.Clear();
            
            // Show today's goals after a short delay
            Invoke("ShowTodaysGoals", 1f);
        }
        else
        {
            // User doesn't want to move goals
            chatUIManager.AddAppMessage("No problem. Would you like to set new goals for today instead?");
            _pendingIncompleteGoals.Clear();
        }
    }
    
    // Display all of today's goals
    public void ShowTodaysGoals()
    {
        DatabaseManager.Instance.LoadGoalsFromFirebase(goalList =>
        {
            if (goalList.goals.Count == 0)
            {
                chatUIManager.AddAppMessage("You don't have any goals set for today.");
                return;
            }
            
            string message = "📋 Your Goals for Today:\n";
            DateTime currentTime = DateTime.Now;
            string today = currentTime.ToString("yyyy-MM-dd"); // Today's date
            List<string> upcomingGoals = new List<string>();
            List<string> completedGoals = new List<string>();
            List<string> noTimingGoals = new List<string>();
            
            foreach (Goal goal in goalList.goals)
            {
                // Skip goals with specific dates that are not today
                if (HasSpecificDate(goal.timing) && !ContainsDate(goal.timing, today)) continue;
                
                string goalInfo = $"• {goal.text}";
                
                if (goal.completed)
                {
                    completedGoals.Add(goalInfo);
                }
                else if (string.IsNullOrEmpty(goal.timing))
                {
                    noTimingGoals.Add(goalInfo);
                }
                else
                {
                    // Extract just the time part if there's a date
                    string displayTiming = goal.timing;
                    if (HasSpecificDate(goal.timing))
                    {
                        // Try to extract just the time portion
                        int timeIndex = goal.timing.IndexOf(" at ");
                        if (timeIndex >= 0)
                        {
                            displayTiming = goal.timing.Substring(timeIndex + 4);
                        }
                    }
                    
                    // Try to parse the time to show how much time is left
                    if (TryParseExactTime(displayTiming, currentTime, out DateTime targetTime))
                    {
                        string timeInfo = $" (at {targetTime.ToString("h:mm tt")})";
                        upcomingGoals.Add(goalInfo + timeInfo);
                    }
                    else
                    {
                        upcomingGoals.Add($"{goalInfo} ({displayTiming})");
                    }
                }
            }
            
            if (upcomingGoals.Count == 0 && completedGoals.Count == 0 && noTimingGoals.Count == 0)
            {
                chatUIManager.AddAppMessage("You don't have any goals set for today.");
                return;
            }
            
            // Add upcoming goals
            if (upcomingGoals.Count > 0)
            {
                message += "\nUpcoming:\n" + string.Join("\n", upcomingGoals);
            }
            
            // Add goals without timing
            if (noTimingGoals.Count > 0)
            {
                message += "\n\nNo Timing Set:\n" + string.Join("\n", noTimingGoals);
            }
            
            // Add completed goals
            if (completedGoals.Count > 0)
            {
                message += "\n\nCompleted:\n" + string.Join("\n", completedGoals);
            }
            
            Debug.Log(message);
            chatUIManager.AddAppMessage(message);
        });
    }

    private bool IsTimeToRemind(string timingString, DateTime currentTime)
    {
        try
        {
            // Handle different time formats
            if (TryParseExactTime(timingString, currentTime, out DateTime targetTime))
            {
                // Check if current time matches target time (within the minute)
                return currentTime.Hour == targetTime.Hour && 
                       currentTime.Minute == targetTime.Minute;
            }
            
            // Handle time ranges (e.g., "9am-1pm")
            if (timingString.Contains("-"))
            {
                string[] range = timingString.Split('-');
                if (range.Length == 2)
                {
                    if (TryParseExactTime(range[0].Trim(), currentTime, out DateTime startTime) &&
                        TryParseExactTime(range[1].Trim(), currentTime, out DateTime endTime))
                    {
                        // If we're at the start time exactly
                        if (currentTime.Hour == startTime.Hour && currentTime.Minute == startTime.Minute)
                            return true;
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse timing: {timingString}. Error: {ex.Message}");
            return false;
        }
    }

    private bool TryParseExactTime(string timeString, DateTime currentDate, out DateTime result)
    {
        result = currentDate;
        timeString = timeString.ToLower().Trim();
        
        // Try common formats
        string[] formats = new string[] 
        { 
            "h:mmtt", "h:mm tt", "htt", "h tt", "h:mm", "HH:mm",
            "h.mmtt", "h.mm tt", "h.mm"
        };
        
        // Replace common variations
        timeString = timeString
            .Replace("am", "AM")
            .Replace("pm", "PM")
            .Replace("a.m.", "AM")
            .Replace("p.m.", "PM")
            .Replace("a.m", "AM")
            .Replace("p.m", "PM");
        
        // Try to parse the time
        foreach (string format in formats)
        {
            if (DateTime.TryParseExact(timeString, format, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out DateTime parsedTime))
            {
                // Combine the parsed time with today's date
                result = new DateTime(
                    currentDate.Year, 
                    currentDate.Month, 
                    currentDate.Day,
                    parsedTime.Hour,
                    parsedTime.Minute,
                    0
                );
                return true;
            }
        }
        
        // Handle special cases like "morning", "evening", etc.
        switch (timeString)
        {
            case "morning":
                result = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 9, 0, 0);
                return true;
            case "afternoon":
                result = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 14, 0, 0);
                return true;
            case "evening":
                result = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 18, 0, 0);
                return true;
            case "night":
                result = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 20, 0, 0);
                return true;
        }
        
        return false;
    }
}