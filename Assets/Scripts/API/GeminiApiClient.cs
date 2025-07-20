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

            Debug.Log("Raw Gemini API Response: " + www.downloadHandler.text);
            GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(www.downloadHandler.text);
            string rawText = res.candidates[0].content.parts[0].text;
            string cleanedJson = JsonHelper.CleanMarkdownJson(rawText);

            // The 'response' action is for displaying text to the user.
            // We generate a human-readable message for it.

            Debug.Log(cleanedJson);
            // string displayMessage = JsonHelper.ExtractHumanReadableContent(cleanedJson);
            if (response != null && !string.IsNullOrEmpty(cleanedJson))
            {
                response.Invoke(cleanedJson);
            }

            // The 'onJsonReply' action is for processing data.
            // We pass the raw, cleaned JSON to it.
            if (onJsonReply != null)
            {
                onJsonReply.Invoke(cleanedJson);
            }
        }
        else
        {
            Debug.LogError("Gemini API Error: " + www.error);
            response?.Invoke("Error: " + www.error);
        }
    }


}
