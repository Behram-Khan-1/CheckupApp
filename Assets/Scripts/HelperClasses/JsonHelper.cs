using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class JsonHelper
{
    public static string CleanMarkdownJson(string raw)
    {
        string cleaned = raw
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // Check if the cleaned string is a JSON object or array
        if ((cleaned.StartsWith("{") && cleaned.EndsWith("}")) ||
            (cleaned.StartsWith("[") && cleaned.EndsWith("]")))
        {
            // This is likely a JSON response, extract the content for display
            return ExtractHumanReadableContent(cleaned);
        }

        return cleaned;
    }

    // Extract human-readable content from JSON responses
    public static string ExtractHumanReadableContent(string json)
    {
        Debug.Log("🧪 Extracting JSON: " + json);

        try
        {
            // Check for common patterns in our JSON responses

            // If it's an intent classification response, don't display it directly
            if (json.Contains("\"intent\":") && json.Contains("\"details\":"))
            {
                return "I'm processing your message...";
            }

            // If it's a goals response, extract the goal texts and timing information
            if (json.Contains("\"goal\":"))
            {
                // Use regex to extract goal texts and timing
                Regex goalTextRegex = new Regex("\"text\":\\s*\"([^\"]+)\"");
                Regex goalTimingRegex = new Regex("\"timing\":\\s*\"([^\"]*)\"");

                MatchCollection textMatches = goalTextRegex.Matches(json);
                MatchCollection timingMatches = goalTimingRegex.Matches(json);

                if (textMatches.Count > 0)
                {
                    string response = "I've added these goals:";

                    for (int i = 0; i < textMatches.Count; i++)
                    {
                        string goalText = textMatches[i].Groups[1].Value;
                        string timing = (i < timingMatches.Count) ? timingMatches[i].Groups[1].Value : "";

                        response += "\n• " + goalText;

                        if (!string.IsNullOrEmpty(timing))
                        {
                            response += " (" + timing + ")";
                        }
                    }

                    response += "\n\nI'll remind you about these goals at the appropriate times!";
                    return response;
                }
            }

            // For tomorrow's goals
            if (json.Contains("\"goal\":") && json.Contains("tomorrow"))
            {
                Regex goalTextRegex = new Regex("\"text\":\\s*\"([^\"]+)\"");
                MatchCollection matches = goalTextRegex.Matches(json);

                if (matches.Count > 0)
                {
                    string response = "I've set these goals for tomorrow:";
                    foreach (Match match in matches)
                    {
                        response += "\n• " + match.Groups[1].Value;
                    }
                    return response;
                }
            }

            // For responses about moving goals
            if (json.Contains("move") && json.Contains("goals") && json.Contains("today"))
            {
                return "I'll update your goals based on your response.";
            }

            // For other JSON responses, return a generic message
            return "I've processed your request.";
        }
        catch (System.Exception ex)
        {
            // If any error occurs during parsing, log it and return the original JSON
            Debug.LogError("Error extracting content from JSON: " + ex.Message);
            return json;
        }
    }
}
