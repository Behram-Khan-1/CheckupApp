using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiApiClient : MonoBehaviour
{ 
    private const string apiKey = "AIzaSyDcJCyS3nAuBUrmLPzgKQAbGM-T3WZGh_Y";
    private string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    public void SendPrompt(string prompt, Action<string> onTextResponse = null, Action<string> onJsonReply = null)
    {
        StartCoroutine(SendPromptCoroutine(prompt, onTextResponse, onJsonReply));
    }


    public IEnumerator SendPromptCoroutine(string prompt, Action<string> response, Action<string> onJsonReply = null)
    {
        var request = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content{
                    parts = new Part[]
                    {
                        new Part
                        {
                            text = prompt
                        }
                    }
                }
            }
        };
        string json = JsonUtility.ToJson(request);
        byte[] jsonbytes = Encoding.UTF8.GetBytes(json);

        UnityWebRequest www = new UnityWebRequest(url + apiKey, "POST");
        www.uploadHandler = new UploadHandlerRaw(jsonbytes);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {

            GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(www.downloadHandler.text);
            var cleanString = JsonHelper.CleanMarkdownJson(res.candidates[0].content.parts[0].text);
            string reply = cleanString;
            // Debug.Log(reply);
            response?.Invoke(reply);

            // If caller wants JSON processing
            if (onJsonReply != null)
            {
                Debug.Log("Raw Gemini reply:\n" + reply);
                onJsonReply.Invoke(reply);
            }
        }
        else
        {
            Debug.LogError("Gemini API Error: " + www.error);
            response?.Invoke("Error: " + www.error);
        }
    }


}
