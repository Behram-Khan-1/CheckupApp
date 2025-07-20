using UnityEngine;
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

    // The Update, ReplyTimer, and SaveGoals methods have been removed to prevent
    // conflicts with the new intent-based system in ChatInputManager.

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