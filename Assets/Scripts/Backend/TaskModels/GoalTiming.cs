
using System;
using System.Collections.Generic;

[System.Serializable]
public class GoalTiming
{
    public string text;
    public string timing;
}

[System.Serializable]
public class GoalTimingList
{
    public List<GoalTiming> goalTimings;
}