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
        // CombineUserMessage();
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

