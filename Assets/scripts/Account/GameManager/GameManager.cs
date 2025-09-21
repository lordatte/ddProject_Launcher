using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private bool waitingForCharacterCheck = false;

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

    private void Start()
    {
        // Subscribe to character check events
        if (CheckCharacters.Instance != null)
        {
            CheckCharacters.Instance.OnCharactersChecked += HandleCharactersChecked;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (CheckCharacters.Instance != null)
        {
            CheckCharacters.Instance.OnCharactersChecked -= HandleCharactersChecked;
        }
    }

    public void Play()
    {
        string server = "server1";

        // Check if there's a logged in account
        if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
        {
            Debug.Log($"Playing on {server} with account: {AccountManager.Instance.CurrentEmail}");

            // Set flag that we're waiting for character check
            waitingForCharacterCheck = true;

            // Check for characters - the result will be handled by HandleCharactersChecked
            CheckCharacters.Instance.CheckForCharacters();
        }
        else
        {
            Debug.LogWarning("Cannot play - no account is logged in!");
            ShowLoginRequiredMessage();
        }
    }

    private void HandleCharactersChecked(bool hasCharacters)
    {
        // Only proceed if we're waiting for a character check from the Play button
        if (!waitingForCharacterCheck) return;

        waitingForCharacterCheck = false;
        Debug.Log($"Characters checked: {hasCharacters}");

        if (hasCharacters)
        {
            LoadScene3();
        }
        else
        {
            LoadScene1();
        }
    }

    public void LoadScene1()
    {
        SceneManager.LoadScene(1); // Scene 1 - Character creation or selection
    }

    public static void LoadScene3()
    {
        SceneManager.LoadScene(3); // Scene 2 - Main game scene
    }
    public void ReturnToLauncher()
    {
        SceneManager.LoadScene(0); // Scene 0 is your launcher/auth scene
    }

    private void ShowLoginRequiredMessage()
    {
        Debug.Log("Please log in first to play!");
        // Implement UI feedback here
    }
}