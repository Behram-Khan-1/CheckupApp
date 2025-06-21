using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Goal
{
    public string text;
    // public string type; // e.g., "habit", "daily", "one-time"
    // public string priority; // e.g., "high", "medium", "low"
    // public bool streak;

    public string timing;    // e.g., "07:00 AM", "Evening", etc.
    public string time = DateTime.Now.ToString("h:mmtt dd MMMM dddd yyyy");
    public bool completed;   // has the user done it today?\
}

[System.Serializable]
public class GoalList
{
    public List<Goal> goals;
}
