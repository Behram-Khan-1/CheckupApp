using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // For TextMeshPro input and text, if you want nicer text

public class ChatManager : MonoBehaviour
{
    public Transform chatContent; // Content object in ScrollView
    public GameObject userBubblePrefab; // Prefab for user messages
    public GameObject appBubblePrefab; // Prefab for app messages
    public TMP_InputField inputField; // User input field
    string userMessage;
    //Timer stuff
    [SerializeField] float initialTime = 3f;
    private float time;

    private bool isTyping = false;
    private bool messageSent = false; //For User Message Checking
    private bool replySent = false; //For AI Message Checking

    // API Portion
    GeminiApiClient geminiApiClient;

    //Prompt Stuff

    PromptBuilder promptBuilder;

    void Start()
    {
        geminiApiClient = gameObject.AddComponent<GeminiApiClient>();
        promptBuilder = new PromptBuilder();
        time = initialTime;
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    void Update()
    {
        if (messageSent)
        {
            ReplyTimer();
        }
    }

    public void OnSendButtonClicked()
    {
        userMessage = inputField.text;
        // CombineUserMessage();
        promptBuilder.Append(userMessage);
        if (!string.IsNullOrEmpty(userMessage) && !string.IsNullOrWhiteSpace(userMessage))
        {
            AddUserMessage(userMessage);
            inputField.text = "";
            messageSent = true;
            isTyping = false;
            replySent = false;
        }
    }



    void AddUserMessage(string message)
    {
        GameObject bubble = Instantiate(userBubblePrefab, chatContent);
        bubble.transform.SetAsLastSibling();
        bubble.GetComponentInChildren<TMP_Text>().text = message;
    }

    void AddAppMessage(string message)
    {
        GameObject bubble = Instantiate(appBubblePrefab, chatContent);
        bubble.transform.SetAsLastSibling();
        bubble.GetComponentInChildren<TMP_Text>().text = message;
    }

    private void ReplyTimer()
    {
        if (isTyping == false)
        {
            // Debug.Log(time);
            time = Timer.TimerStart(time);
            if (time <= 0 && replySent == false)
            {
                messageSent = false;
                replySent = true;
                time = initialTime; //Later make send button unclickable when not received reply
                StartCoroutine(geminiApiClient.SendPrompt(promptBuilder.BuildGoalExtractionPrompt(), AddAppMessage, DisplayGoals));
                promptBuilder.Reset();
            }
        }
        else
        {
            time = initialTime;
        }
    }

    /// <summary>
    /// Called when the input field value changes. Sets the isTyping flag based on whether
    /// the input field has text. If the input field is empty and a message has been sent,
    /// sets isTyping to false.
    /// </summary>
    /// <param name="text">The current text in the input field.</param>

    private void OnValueChanged(string text)
    {
        if (inputField.text.Length > 0)
        {
            isTyping = true;
        }
        if (inputField.text.Length == 0 && messageSent == true)
        {
            isTyping = false;
        }
    }

    public class Timer
    {
        public static float TimerStart(float time)
        {
            time = time - Time.deltaTime;

            return time;
        }
    }

    public void DisplayGoals(string jsonResponse)
    {
        try
        {
            GoalList goalList = JsonUtility.FromJson<GoalList>(jsonResponse);

            foreach (Goal goal in goalList.goals)
            {
                string goalText = $"- {goal.text} [{goal.type}] Priority: {goal.priority}, Streak: {goal.streak}";
                Debug.Log(goalText);
                // AddAppMessage(goalText);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse goal JSON: " + ex.Message);
            AddAppMessage("⚠️ Couldn't understand your goals. Try rephrasing.");
        }
    }


}