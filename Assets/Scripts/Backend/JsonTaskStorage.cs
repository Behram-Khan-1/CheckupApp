using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class JsonTaskStorage
{
    private static string path = Path.Combine(Application.persistentDataPath, "tasks.json");

    public static void SaveTasks(GoalList goalList)
    {
        

        Debug.Log("Saving tasks");
        string json = JsonUtility.ToJson(goalList, true);
        File.WriteAllText(path, json);
    }

    public static GoalList LoadTasks()
    {
        if (File.Exists(path) == false)
        {
            return new GoalList();
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<GoalList>(json);

    }

    public static void UpdateTasksTiming(List<Goal> newGoal)
    {
        GoalList goalList = LoadTasks();
        for (int i = 0; i < newGoal.Count; i++)
        {
            var match = goalList.goals.FirstOrDefault(g => g.text == newGoal[i].text);
            if (match != null)
            {
                match.timing = newGoal[i].timing;
            }
        }
        SaveTasks(goalList);
    }


}
