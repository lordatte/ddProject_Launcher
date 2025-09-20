using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    public string CurrentEmail { get; private set; }
    public string PlayFabId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public event Action<string, string> OnLoginSuccess;
    public event Action OnLogout;

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

    public void SetLoggedInAccount(string email, string playFabId)
    {
        CurrentEmail = email;
        PlayFabId = playFabId;
        IsLoggedIn = true;
        Debug.Log($"Account set as logged in: {email}, PlayFabId: {playFabId}");

        // Trigger login event
        OnLoginSuccess?.Invoke(email, playFabId);
    }

    public void ClearAccount()
    {
        CurrentEmail = string.Empty;
        PlayFabId = string.Empty;
        IsLoggedIn = false;
        Debug.Log("Account cleared");

        // Trigger logout event
        OnLogout?.Invoke();
    }

    public void UpdatePlayFabId(string playFabId)
    {
        PlayFabId = playFabId;
        Debug.Log($"PlayFabId updated: {playFabId}");
    }
}