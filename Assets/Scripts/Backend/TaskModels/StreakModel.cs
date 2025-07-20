using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Streak
{
    public string name;         // e.g., "nofap", "workout", "meditation"
    public string status;       // e.g., "completed", "failed", "day 5"
    public int currentStreak;   // current streak count
    public int longestStreak;   // longest streak achieved
    public string lastUpdated;  // timestamp of last update
}

[System.Serializable]
public class StreakList
{
    public List<Streak> streaks;
}