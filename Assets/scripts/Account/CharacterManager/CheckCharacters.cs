using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class CheckCharacters : MonoBehaviour
{
    public static CheckCharacters Instance { get; private set; }

    private List<CharacterResult> characterList;
    private bool hasCharacters = false;
    private bool isChecking = false;

    public event System.Action<bool> OnCharactersChecked;

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

    public void CheckForCharacters()
    {
        if (isChecking)
        {
            Debug.Log("Character check already in progress...");
            return;
        }

        if (AccountManager.Instance == null || !AccountManager.Instance.IsLoggedIn || string.IsNullOrEmpty(AccountManager.Instance.PlayFabId))
        {
            Debug.LogWarning("Cannot check characters - no account is logged in!");
            OnCharactersChecked?.Invoke(false);
            return;
        }

        isChecking = true;
        Debug.Log("Checking for characters...");

        var request = new ListUsersCharactersRequest
        {
            PlayFabId = AccountManager.Instance.PlayFabId
        };

        PlayFabClientAPI.GetAllUsersCharacters(request,
            OnGetCharactersSuccess,
            OnGetCharactersFailure
        );
    }

    private void OnGetCharactersSuccess(ListUsersCharactersResult result)
    {
        isChecking = false;
        characterList = result.Characters;
        hasCharacters = result.Characters != null && result.Characters.Count > 0;

        Debug.Log($"Character check successful. Found {result.Characters?.Count ?? 0} characters.");

        // Trigger event for any listeners
        OnCharactersChecked?.Invoke(hasCharacters);
    }

    private void OnGetCharactersFailure(PlayFabError error)
    {
        isChecking = false;
        Debug.LogError($"Failed to get characters: {error.GenerateErrorReport()}");

        // Trigger event with failure (assume no characters)
        OnCharactersChecked?.Invoke(false);
    }

    public bool HasCharacters()
    {
        return hasCharacters;
    }

    public int GetCharacterCount()
    {
        return characterList?.Count ?? 0;
    }

    public List<CharacterResult> GetCharacterList()
    {
        return characterList;
    }

    public CharacterResult GetFirstCharacter()
    {
        if (characterList != null && characterList.Count > 0)
        {
            return characterList[0];
        }
        return null;
    }

    // Clear character data (useful when logging out)
    public void ClearCharacterData()
    {
        characterList = null;
        hasCharacters = false;
        isChecking = false;
    }

    // Optional: Method to refresh character list
    public void RefreshCharacters()
    {
        ClearCharacterData();
        CheckForCharacters();
    }
}