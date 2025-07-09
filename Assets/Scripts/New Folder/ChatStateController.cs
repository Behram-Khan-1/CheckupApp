using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatStateController : MonoBehaviour
{
    //Enums
    [SerializeField] ChatState chatState;
    [SerializeField] ChatMode chatMode;
    public void SetChatMode(ChatMode chatMode)
    {
        this.chatMode = chatMode;
    }
    public ChatMode GetChatMode()
    {
        return chatMode;
    }

    public ChatState GetChatState()
    {
        return chatState;
    }
    public void SetChatState(ChatState chatState)
    {
        this.chatState = chatState;
    }
}
