using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic; // For TextMeshPro input and text, if you want nicer text
using System.Linq;

public class ChatManager : MonoBehaviour
{
    
    //Timer stuff
    [SerializeField] float initialTime = 3f;
    [SerializeField] private float time;

    // API Portion
    GeminiApiClient geminiApiClient;

    //Prompt Stuff
    PromptBuilder promptBuilder;
    //GoalTimnigManager
    GoalTimingManager goalTimingManager;
    [SerializeField] private DatabaseManager databaseManager;
    [SerializeField] private ChatUIManager chatUIManager;
    [SerializeField] private ChatStateController chatStateController;

    void Start()
    {
        geminiApiClient = gameObject.AddComponent<GeminiApiClient>();

        time = initialTime;
        goalTimingManager = new GoalTimingManager(chatStateController, geminiApiClient, chatUIManager);
        
        promptBuilder = PromptService.Instance.promptBuilder;
        
        // Ensure GoalReminderManager is attached
        GoalReminderManager reminderManager = GetComponent<GoalReminderManager>();
        if (reminderManager == null)
        {
            reminderManager = gameObject.AddComponent<GoalReminderManager>();
        }
        
        // Add AudioSource for notification sounds if not present
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
            
            // Try to load a notification sound
            audioSource.clip = Resources.Load<AudioClip>("Sounds/notification");
            if (audioSource.clip == null)
            {
                Debug.LogWarning("Notification sound not found in Resources/Sounds/notification. Please add a sound file.");
            }
        }
    }

    void Update()
    {
        if (chatStateController.GetChatState() == ChatState.MessageSent
             || chatStateController.GetChatState() == ChatState.Typing)
        {
            ReplyTimer();
        }
    }







    private void ReplyTimer()
    {
        if (chatStateController.GetChatState() == ChatState.Typing)
        {
            time = initialTime;
            return;
        }

        // Debug.Log(time);
        time = Timer.TimerStart(time);
        if (time <= 0)
        {
            chatStateController.SetChatState(ChatState.WaitingForReply);
            time = initialTime; //Later make send button unclickable when not received reply
            StartCoroutine(geminiApiClient.SendPromptCoroutine(promptBuilder.BuildPrompt(),
             chatUIManager.AddAppMessage,
              SaveGoals));



            Debug.Log("ReplyTimer Prompt Response");
            promptBuilder.Reset();
        }

    }



    public void SaveGoals(string jsonResponse)
    {
        try
        {
            databaseManager.SaveGoalsToFirebase(JsonUtility.FromJson<GoalList>(jsonResponse));
            Debug.Log("Goals saved to Firebase successfully!");
            goalTimingManager.GoalsNeedingTiming();

            chatStateController.SetChatState( ChatState.Idle);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse goal JSON: " + ex.Message);
            chatUIManager.AddAppMessage("⚠️ Couldn't understand your goals. Try rephrasing.");
        }
    }

    #region Helpers
    /// <summary>
    /// Called when the input field value changes. Sets the isTyping flag based on whether
    /// the input field has text. If the input field is empty and a message has been sent,
    /// sets isTyping to false.
    /// </summary>
    /// <param name="text">The current text in the input field.</param>


    public class Timer
    {
        public static float TimerStart(float time)
        {
            time = time - Time.deltaTime;

            return time;
        }


    }


  
    #endregion
}