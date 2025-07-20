using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;
    private FirebaseFirestore firestore;

    private string userId = "testUser"; // Replace with actual user ID when Auth is added

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                firestore = FirebaseFirestore.DefaultInstance;
                Debug.Log("✅ Firestore initialized!");
            }
            else
            {
                Debug.LogError("❌ Firebase dependency issue: " + task.Result);
            }
        });
    }

    // ✅ Save Goals
    public void SaveGoalsToFirebase(GoalList goalList)
    {
        CollectionReference userGoalsRef = firestore.Collection("users").Document(userId).Collection("goals");

        // Delete previous goals first to avoid duplicates
        DeleteAllUserGoals(() =>
        {
            foreach (Goal goal in goalList.goals)
            {
                Dictionary<string, object> goalData = new Dictionary<string, object>
                {
                    { "text", goal.text },
                    { "timing", goal.timing },
                    { "time", goal.time },
                    { "completed", goal.completed }
                };

                userGoalsRef.AddAsync(goalData).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                        Debug.Log("✅ Goal saved: " + goal.text);
                    else
                        Debug.LogError("❌ Error saving goal: " + task.Exception);
                });
            }
        });
    }

    // ✅ Load Goals
    public void LoadGoalsFromFirebase(Action<GoalList> onLoaded)
    {
        firestore.Collection("users").Document(userId).Collection("goals")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to load goals: " + task.Exception);
                    onLoaded?.Invoke(new GoalList { goals = new List<Goal>() });
                    return;
                }

                List<Goal> loadedGoals = new List<Goal>();
                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    Dictionary<string, object> data = doc.ToDictionary();

                    Goal goal = new Goal
                    {
                        text = data["text"] as string,
                        timing = data.ContainsKey("timing") ? data["timing"] as string : "",
                        time = data.ContainsKey("time") ? data["time"] as string : DateTime.Now.ToString("h:mmtt dd MMMM dddd yyyy"),
                        completed = data.ContainsKey("completed") && (bool)data["completed"]
                    };

                    loadedGoals.Add(goal);
                }

                onLoaded?.Invoke(new GoalList { goals = loadedGoals });
            });
    }

    // ✅ Update Goal Timing
    public void UpdateGoalsTiming(List<Goal> updatedGoals)
    {
        firestore.Collection("users").Document(userId).Collection("goals")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to update goals: " + task.Exception);
                    return;
                }

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    string goalText = doc.ContainsField("text") ? doc.GetValue<string>("text") : null;
                    var match = updatedGoals.Find(g => g.text == goalText);

                    if (match != null)
                    {
                        DocumentReference docRef = doc.Reference;
                        Dictionary<string, object> updates = new Dictionary<string, object>
                        {
                            { "timing", match.timing }
                        };

                        docRef.UpdateAsync(updates).ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.IsCompleted)
                                Debug.Log("✅ Timing updated for: " + goalText);
                            else
                                Debug.LogError("❌ Failed to update timing: " + updateTask.Exception);
                        });
                    }
                }
            });
    }

    // ✅ Mark Goal as Completed
    public void MarkGoalAsCompleted(string goalText, bool isCompleted = true)
    {
        firestore.Collection("users").Document(userId).Collection("goals")
            .WhereEqualTo("text", goalText)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("❌ Failed to find goal: " + task.Exception);
                    return;
                }

                QuerySnapshot querySnapshot = task.Result;

                if (querySnapshot == null || querySnapshot.Count == 0)
                {
                    Debug.LogWarning("⚠️ Goal not found: " + goalText);
                    return;
                }

                foreach (DocumentSnapshot doc in querySnapshot.Documents)
                {
                    DocumentReference docRef = doc.Reference;

                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                    { "completed", isCompleted }
                    };

                    docRef.UpdateAsync(updates).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                            Debug.Log("✅ Goal marked as " + (isCompleted ? "completed" : "incomplete") + ": " + goalText);
                        else
                            Debug.LogError("❌ Failed to update goal: " + updateTask.Exception);
                    });
                }
            });
    }


    // 🔄 Optional Helper: Delete all goals (to avoid duplicates)
    private void DeleteAllUserGoals(Action onDeleted)
    {
        firestore.Collection("users").Document(userId).Collection("goals")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("⚠️ Could not clear old goals.");
                    onDeleted?.Invoke();
                    return;
                }

                var deleteTasks = new List<Task>();
                foreach (var doc in task.Result.Documents)
                {
                    deleteTasks.Add(doc.Reference.DeleteAsync());
                }

                Task.WhenAll(deleteTasks).ContinueWithOnMainThread(_ => onDeleted?.Invoke());
            });
    }
}
