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
                Debug.LogError("Firebase dependency issue: " + task.Result);
            }
        });
    }

    // ✅ Save Goals
    public void SaveGoalsToFirebase(GoalList goalList)
    {
        // Get today's date in YYYY-MM-DD format
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Call the method to save goals for a specific date
        SaveGoalsForDate(goalList, todayDate);
    }
    
    // Save goals for a specific date
    public void SaveGoalsForDate(GoalList goalList, string date)
    {
        // Reference to the date's goals collection
        CollectionReference userGoalsRef = firestore.Collection("users").Document(userId)
            .Collection("goals_by_date").Document(date).Collection("goals");

        // Delete previous goals for this date first to avoid duplicates
        DeleteGoalsForDate(date, () =>
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
                        Debug.Log("Goal saved for " + date + ": " + goal.text);
                    else
                        Debug.LogError("Error saving goal: " + task.Exception);
                });
                
                // Also save to the main goals collection for reference
                firestore.Collection("users").Document(userId).Collection("goals")
                    .AddAsync(goalData).ContinueWithOnMainThread(task =>
                    {
                        if (!task.IsCompleted)
                            Debug.LogError("Error saving goal to main collection: " + task.Exception);
                    });
            }
            
            Debug.Log("✅ All goals saved for date: " + date);
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
                    Debug.LogError("Failed to load goals: " + task.Exception);
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
    
    // Load goals for a specific date
    public void LoadGoalsForDate(string date, Action<GoalList> onGoalsLoaded)
    {
        CollectionReference dateGoalsRef = firestore.Collection("users").Document(userId)
            .Collection("goals_by_date").Document(date).Collection("goals");

        dateGoalsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;
                GoalList goalList = new GoalList { goals = new List<Goal>() };

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    Dictionary<string, object> data = doc.ToDictionary();

                    Goal goal = new Goal
                    {
                        text = data.ContainsKey("text") ? data["text"].ToString() : "",
                        timing = data.ContainsKey("timing") ? data["timing"].ToString() : "",
                        time = data.ContainsKey("time") ? data["time"].ToString() : "",
                        completed = data.ContainsKey("completed") && bool.Parse(data["completed"].ToString())
                    };

                    goalList.goals.Add(goal);
                }

                Debug.Log($"Goals for {date} loaded: " + goalList.goals.Count);
                onGoalsLoaded?.Invoke(goalList);
            }
            else
            {
                Debug.LogError($"Error loading goals for {date}: " + task.Exception);
                onGoalsLoaded?.Invoke(new GoalList { goals = new List<Goal>() });
            }
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
                    Debug.LogError("Failed to update goals: " + task.Exception);
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
                                Debug.Log("Timing updated for: " + goalText);
                            else
                                Debug.LogError("Failed to update timing: " + updateTask.Exception);
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
                    Debug.LogError("Failed to find goal: " + task.Exception);
                    return;
                }

                QuerySnapshot querySnapshot = task.Result;

                if (querySnapshot == null || querySnapshot.Count == 0)
                {
                    Debug.LogWarning("Goal not found: " + goalText);
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
                            Debug.Log("Goal marked as " + (isCompleted ? "completed" : "incomplete") + ": " + goalText);
                        else
                            Debug.LogError("Failed to update goal: " + updateTask.Exception);
                    });
                }
            });
    }


    // Delete all goals for a specific date
    private void DeleteGoalsForDate(string date, Action onComplete)
    {
        CollectionReference dateGoalsRef = firestore.Collection("users").Document(userId)
            .Collection("goals_by_date").Document(date).Collection("goals");

        dateGoalsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;
                int totalDocuments = snapshot.Count;
                int deletedCount = 0;

                if (totalDocuments == 0)
                {
                    onComplete?.Invoke();
                    return;
                }

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    doc.Reference.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                    {
                        deletedCount++;
                        if (deletedCount >= totalDocuments)
                        {
                            Debug.Log($"All goals for {date} deleted");
                            onComplete?.Invoke();
                        }
                    });
                }
            }
            else
            {
                Debug.LogError($"Error getting goals for {date} deletion: " + task.Exception);
                onComplete?.Invoke(); // Still try to continue
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
                    Debug.LogWarning("Could not clear old goals.");
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
    
    #region Streak Management
    
    // Save a streak update to Firebase
    public void SaveStreakUpdate(Streak streak)
    {
        DocumentReference streakRef = firestore.Collection("users").Document(userId)
            .Collection("streaks").Document(streak.name.ToLower());
            
        // First check if the streak already exists
        streakRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;
                Dictionary<string, object> streakData = new Dictionary<string, object>
                {
                    { "name", streak.name },
                    { "status", streak.status },
                    { "lastUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };
                
                // If streak exists, update it
                if (snapshot.Exists)
                {
                    Dictionary<string, object> existingData = snapshot.ToDictionary();
                    int currentStreak = 0;
                    int longestStreak = 0;
                    
                    // Get existing streak counts
                    if (existingData.ContainsKey("currentStreak"))
                        int.TryParse(existingData["currentStreak"].ToString(), out currentStreak);
                        
                    if (existingData.ContainsKey("longestStreak"))
                        int.TryParse(existingData["longestStreak"].ToString(), out longestStreak);
                    
                    // Update streak count based on status
                    if (streak.status.ToLower().Contains("completed") || 
                        streak.status.ToLower().Contains("day"))
                    {
                        currentStreak++;
                        if (currentStreak > longestStreak)
                            longestStreak = currentStreak;
                    }
                    else if (streak.status.ToLower().Contains("failed") || 
                             streak.status.ToLower().Contains("reset"))
                    {
                        currentStreak = 0;
                    }
                    
                    // Add streak counts to data
                    streakData["currentStreak"] = currentStreak;
                    streakData["longestStreak"] = longestStreak;
                    
                    // Update the streak document
                    streakRef.UpdateAsync(streakData).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                            Debug.Log($"Streak updated: {streak.name} - Current: {currentStreak}, Longest: {longestStreak}");
                        else
                            Debug.LogError($"Error updating streak: {updateTask.Exception}");
                    });
                }
                else
                {
                    // Create new streak with initial values
                    int currentStreak = 0;
                    if (streak.status.ToLower().Contains("completed") || 
                        streak.status.ToLower().Contains("day"))
                    {
                        currentStreak = 1;
                    }
                    
                    streakData["currentStreak"] = currentStreak;
                    streakData["longestStreak"] = currentStreak;
                    
                    // Create the streak document
                    streakRef.SetAsync(streakData).ContinueWithOnMainThread(setTask =>
                    {
                        if (setTask.IsCompleted)
                            Debug.Log($"New streak created: {streak.name} - Current: {currentStreak}");
                        else
                            Debug.LogError($"Error creating streak: {setTask.Exception}");
                    });
                }
            }
            else
            {
                Debug.LogError($"Error checking streak existence: {task.Exception}");
            }
        });
    }
    
    // Load all streaks from Firebase
    public void LoadStreaks(Action<StreakList> onStreaksLoaded)
    {
        CollectionReference streaksRef = firestore.Collection("users").Document(userId).Collection("streaks");
        
        streaksRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;
                StreakList streakList = new StreakList { streaks = new List<Streak>() };
                
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    Dictionary<string, object> data = doc.ToDictionary();
                    
                    Streak streak = new Streak
                    {
                        name = data.ContainsKey("name") ? data["name"].ToString() : "",
                        status = data.ContainsKey("status") ? data["status"].ToString() : "",
                        lastUpdated = data.ContainsKey("lastUpdated") ? data["lastUpdated"].ToString() : ""
                    };
                    
                    // Parse streak counts
                    if (data.ContainsKey("currentStreak"))
                        int.TryParse(data["currentStreak"].ToString(), out streak.currentStreak);
                        
                    if (data.ContainsKey("longestStreak"))
                        int.TryParse(data["longestStreak"].ToString(), out streak.longestStreak);
                    
                    streakList.streaks.Add(streak);
                }
                
                Debug.Log($"Streaks loaded: {streakList.streaks.Count}");
                onStreaksLoaded?.Invoke(streakList);
            }
            else
            {
                Debug.LogError($"Error loading streaks: {task.Exception}");
                onStreaksLoaded?.Invoke(new StreakList { streaks = new List<Streak>() });
            }
        });
    }
    
    #endregion
}
