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
    [SerializeField] private float time;

    [SerializeField] ChatState chatState;

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
        if (chatState == ChatState.MessageSent || chatState == ChatState.Typing)
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
            chatState = ChatState.MessageSent;

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
        if (chatState == ChatState.Typing)
        {
            time = initialTime;
            return;
        }

        // Debug.Log(time);
        time = Timer.TimerStart(time);
        if (time <= 0)
        {
            chatState = ChatState.WaitingForReply;
            time = initialTime; //Later make send button unclickable when not received reply
            StartCoroutine(geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(),
             AddAppMessage,
              DisplayGoals));
            promptBuilder.Reset();
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
        if (text.Length > 0)
        {
            chatState = ChatState.Typing;
        }
        if (string.IsNullOrEmpty(text))
        {
            chatState = ChatState.Idle;
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
            JsonTaskStorage.SaveTasks(JsonUtility.FromJson<GoalList>(jsonResponse));
            var x = JsonTaskStorage.LoadTasks();
            

            foreach (Goal goal in x.goals)
            {
                string goalText = $"- {goal.text}, [{goal.timing}], Priority: {goal.completed}";
                Debug.Log(goalText);

                // AddAppMessage(goalText);
            }
                chatState = ChatState.Idle;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse goal JSON: " + ex.Message);
            AddAppMessage("⚠️ Couldn't understand your goals. Try rephrasing.");
        }
    }



}