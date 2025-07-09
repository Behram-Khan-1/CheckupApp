using UnityEngine;

public class PromptService : MonoBehaviour
{
    public static PromptService Instance;

    public PromptBuilder promptBuilder;

    void Awake()
    {
        // Ensure that only one instance of PromptService exists
        if (Instance == null)
        {

            promptBuilder = new PromptBuilder();
            Instance = this;
        }
        else
            Destroy(gameObject); // prevent duplicates
    }
}
