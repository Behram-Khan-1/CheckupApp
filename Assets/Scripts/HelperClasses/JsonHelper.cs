using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JsonHelper
{
    public static string CleanMarkdownJson(string raw)
    {
         return raw
        .Replace("```json", "")
        .Replace("```", "")
        .Trim();
    }
}
