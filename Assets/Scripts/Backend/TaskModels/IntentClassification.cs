using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IntentClassification
{
    public string intent;
    public GoalIntent goal;
    public StreakIntent streak;
}

[System.Serializable]
public class GoalIntent
{
    public string text;
    public string timing;
}

[System.Serializable]
public class StreakIntent
{
    public string name;
    public string status;
}