using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginCheck : MonoBehaviour
{
    [SerializeField] private int launcherSceneIndex = 0; // Scene index for the launcher
    [SerializeField] private bool checkOnStart = true; // Whether to check immediately on Start
    [SerializeField] private bool checkOnUpdate = false; // Whether to continuously check (optional)

    private void Start()
    {
        if (checkOnStart)
        {
            CheckLoginStatus();
        }
    }

    private void Update()
    {
        if (checkOnUpdate)
        {
            CheckLoginStatus();
        }
    }

    public void CheckLoginStatus()
    {
        if (!IsLoggedIn())
        {
            Debug.LogWarning("User not logged in. Returning to launcher...");
            ReturnToLauncher();
        }
        else
        {
            Debug.Log("User is logged in. Proceeding with game...");
        }
    }

    private bool IsLoggedIn()
    {
        // Check if AccountManager exists and user is logged in
        if (AccountManager.Instance == null)
        {
            Debug.LogWarning("AccountManager instance not found!");
            return false;
        }

        return AccountManager.Instance.IsLoggedIn;
    }

    private void ReturnToLauncher()
    {
        SceneManager.LoadScene(launcherSceneIndex);
    }

    // Optional: Public method to manually trigger login check
    public void ForceLoginCheck()
    {
        CheckLoginStatus();
    }
}