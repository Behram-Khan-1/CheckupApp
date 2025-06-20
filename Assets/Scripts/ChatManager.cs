using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic; // For TextMeshPro input and text, if you want nicer text
using System.Linq;

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
    //Enums
    [SerializeField] ChatState chatState;
    [SerializeField] ChatMode chatMode;

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

            if (chatMode == ChatMode.AwaitingGoalTiming)
            {
                SetGoalsTiming(userMessage);
            }
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
              SaveGoals));



            Debug.Log("ReplyTimer Prompt Response");
            promptBuilder.Reset();
        }

    }



    public void SaveGoals(string jsonResponse)
    {
        try
        {
            JsonTaskStorage.SaveTasks(JsonUtility.FromJson<GoalList>(jsonResponse));
            GoalList goals = JsonTaskStorage.LoadTasks();
            GoalsNeedingTiming();

            chatState = ChatState.Idle;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse goal JSON: " + ex.Message);
            AddAppMessage("⚠️ Couldn't understand your goals. Try rephrasing.");
        }
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
        chatMode = ChatMode.AwaitingGoalTiming;

        // foreach (Goal goal in goalsNeedingTiming)
        // {
        //     string goalText = $"- {goal.text}, [{goal.timing}]";
        //     Debug.Log(goalText);
        // }

        promptBuilder.Reset();
        foreach (Goal goal in goalsNeedingTiming)
        {
            promptBuilder.Append(goal.text);
        }

        StartCoroutine(geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(), AddAppMessage));

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

        StartCoroutine(geminiApiClient.SendPrompt(promptBuilder.BuildPrompt(), AddAppMessage, UpdateGoaltiming));
        chatMode = ChatMode.Normal;
        chatState = ChatState.Idle;
    }

    #region Helpers
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

    // public void ClearChat()
    // {
    //     foreach (Transform child in chatContent)
    //     {
    //         GameObject.Destroy(child.gameObject);
    //     }
    // }

    public void UpdateGoaltiming(string jsonResponse)
    {
        GoalList goalList = JsonTaskStorage.LoadTasks();
        List<Goal> goalsNeedingTiming = goalList.goals.Where(g => string.IsNullOrEmpty(g.timing)).ToList();

        var response = JsonUtility.FromJson<GoalList>(jsonResponse);


        foreach (Goal goal in goalsNeedingTiming)
        {
            goal.timing = response.goals.Find(g => g.text == goal.text).timing;
        }

        JsonTaskStorage.UpdateTasksTiming(goalsNeedingTiming);

    }
    #endregion
}