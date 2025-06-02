using UnityEngine;
using UnityEngine.UI;
using TMPro; // For TextMeshPro input and text, if you want nicer text

public class ChatManager : MonoBehaviour
{
    public Transform chatContent; // Content object in ScrollView
    public GameObject userBubblePrefab; // Prefab for user messages
    public GameObject appBubblePrefab; // Prefab for app messages
    public TMP_InputField inputField; // User input field

    public void OnSendButtonClicked()
    {
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage))
        {
            AddUserMessage(userMessage);
            inputField.text = "";

            // Here you can add app reply logic later
            AddAppMessage("Great! I'll remember that.");
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
}
