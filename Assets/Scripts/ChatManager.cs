using UnityEngine;
using UnityEngine.UI;
using TMPro; // For TextMeshPro input and text, if you want nicer text

public class ChatManager : MonoBehaviour
{
    public Transform chatContent; // Content object in ScrollView
    public GameObject userBubblePrefab; // Prefab for user messages
    public GameObject appBubblePrefab; // Prefab for app messages
    public TMP_InputField inputField; // User input field

    [SerializeField] float initialTime = 3f;
    private float time;

    private bool isTyping = false;
    private bool messageSent = false; //For User Message Checking
    private bool replySent = false; //For User Message Checking
    void Start()
    {
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
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage))
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
            Debug.Log(time);
            time = Timer.TimerStart(time);
            if (time <= 0 && replySent == false)
            {
                AddAppMessage("Hello! How can I help you today?");
                messageSent = false;
                replySent = true;
                time = initialTime;
            }
        }
        else
        {
            time = initialTime;
        }
    }

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
}