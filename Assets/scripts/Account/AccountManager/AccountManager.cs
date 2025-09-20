using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    public string CurrentEmail { get; private set; }
    public bool IsLoggedIn { get; private set; }

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

    public void SetLoggedInAccount(string email)
    {
        CurrentEmail = email;
        IsLoggedIn = true;
        Debug.Log($"Account set as logged in: {email}");
    }

    public void ClearAccount()
    {
        CurrentEmail = string.Empty;
        IsLoggedIn = false;
        Debug.Log("Account cleared");
    }
}