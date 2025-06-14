using System.Collections;
using System.Collections.Generic;
using System.IO;
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


}
