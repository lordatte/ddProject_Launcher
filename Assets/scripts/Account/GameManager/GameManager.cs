using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play()
    {
        string server = "server1";

        // Check if there's a logged in account
        if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
        {
            Debug.Log($"Playing on {server} with account: {AccountManager.Instance.CurrentEmail}");
            LoadMainScene();
        }
        else
        {
            Debug.LogWarning("Cannot play - no account is logged in!");
            // You might want to show a UI message here or redirect to login
            ShowLoginRequiredMessage();
        }
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene(1); // Assuming scene 1 is your main game scene
    }

    public void ReturnToLauncher()
    {
        SceneManager.LoadScene(0); // Scene 0 is your launcher/auth scene
    }

    private void ShowLoginRequiredMessage()
    {
        // You can implement UI feedback here, like showing a popup or error message
        Debug.Log("Please log in first to play!");

        // Optional: You could trigger an event or call a UI method here
        // For example: UIManager.Instance.ShowLoginRequiredPopup();
    }
}