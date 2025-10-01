using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using TMPro;
using System.Collections.Generic;

public class CreationCharacterConfirm : MonoBehaviour
{
    [Header("PlayFab Settings")]
    [SerializeField] private string maleItemId = "CharacterMale";
    [SerializeField] private string femaleItemId = "CharacterFemale";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Dependencies")]
    [SerializeField] private CharacterCreationController characterCreationController;

    private string characterName;
    private string characterGender;

    public void InitializeCharacterCreation(string name, string gender)
    {
        characterName = name;
        characterGender = gender;

        CheckInventoryAndCreateCharacter();
    }

    private void CheckInventoryAndCreateCharacter()
    {
        ShowLoading("Checking inventory...");

        var request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request,
            OnGetInventorySuccess,
            OnGetInventoryFailure
        );
    }

    private void OnGetInventorySuccess(GetUserInventoryResult result)
    {
        bool hasMaleItem = HasItem(result.Inventory, maleItemId);
        bool hasFemaleItem = HasItem(result.Inventory, femaleItemId);

        if ((characterGender == "male" && hasMaleItem) ||
            (characterGender == "female" && hasFemaleItem))
        {
            CreatePlayFabCharacter();
        }
        else
        {
            string requiredItem = characterGender == "male" ? maleItemId : femaleItemId;
            HideLoading();
            ShowError($"You don't have the required gender item: {requiredItem}");
            Debug.LogError($"Missing required item: {requiredItem} for gender: {characterGender}");

            // Notify the character creation controller
            if (characterCreationController != null)
            {
                characterCreationController.ShowCreationError($"Missing required item: {requiredItem}");
            }
        }
    }

    private bool HasItem(List<ItemInstance> inventory, string itemId)
    {
        if (inventory == null) return false;

        foreach (var item in inventory)
        {
            if (item.ItemId == itemId)
            {
                return true;
            }
        }
        return false;
    }

    private void OnGetInventoryFailure(PlayFabError error)
    {
        HideLoading();
        string errorMessage = $"Failed to check inventory: {error.ErrorMessage}";
        ShowError(errorMessage);
        Debug.LogError($"PlayFab Inventory Error: {error.GenerateErrorReport()}");

        // Notify the character creation controller
        if (characterCreationController != null)
        {
            characterCreationController.ShowCreationError(errorMessage);
        }
    }

    private void CreatePlayFabCharacter()
    {
        ShowLoading("Creating character...");

        // Use GrantCharacterToUser which is more commonly available
        var request = new GrantCharacterToUserRequest
        {
            CharacterName = characterName,
            ItemId = characterGender == "male" ? maleItemId : femaleItemId
        };

        PlayFabClientAPI.GrantCharacterToUser(request,
            OnCreateCharacterSuccess,
            OnCreateCharacterFailure
        );
    }

    private void OnCreateCharacterSuccess(GrantCharacterToUserResult result)
    {
        HideLoading();
        string successMessage = $"Character '{characterName}' created successfully!";
        ShowSuccess(successMessage);
        Debug.Log($"Character created: {result.CharacterId}");

        // Refresh characters first, then fill data
        if (CharacterManager.Instance != null)
        {
            CheckCharacters.Instance.RefreshCharacters();
            // Wait a moment then fill data
            StartCoroutine(WaitAndFillCharacterData());
        }

        if (characterCreationController != null)
        {
            characterCreationController.ShowCreationSuccess(successMessage);
        }

        // Don't load scene immediately, wait for data to be filled
        StartCoroutine(WaitAndLoadScene());
    }

    private System.Collections.IEnumerator WaitAndFillCharacterData()
    {
        yield return new WaitForSeconds(1f); // Wait for refresh to complete
        CharacterManager.Instance.FillCharacterData();
    }

    private System.Collections.IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(1.5f); // Wait for data to be filled
        GameManager.LoadScene3();
    }
    private void OnCreateCharacterFailure(PlayFabError error)
    {
        HideLoading();
        string errorMessage;

        // Handle common PlayFab errors
        switch (error.Error)
        {
            case PlayFabErrorCode.ProfaneDisplayName:
                errorMessage = "Character name contains inappropriate content.";
                break;
            case PlayFabErrorCode.NameNotAvailable:
                errorMessage = "Character name is already taken. Please choose another name.";
                break;
            case PlayFabErrorCode.CharacterNotFound:
                errorMessage = "Character creation failed. Please try again.";
                break;
            default:
                errorMessage = $"Failed to create character: {error.ErrorMessage}";
                break;
        }

        ShowError(errorMessage);
        Debug.LogError($"PlayFab Character Creation Error: {error.GenerateErrorReport()}");

        // Notify the character creation controller
        if (characterCreationController != null)
        {
            characterCreationController.ShowCreationError(errorMessage);
        }
    }

    private void ShowLoading(string message)
    {
        if (statusText != null) statusText.text = message;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
    }

    private void HideLoading()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    private void ShowSuccess(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = Color.green;
        }
    }

    private void ShowError(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = Color.red;
        }
    }

    public void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.color = Color.white;
        }
        HideLoading();
    }

    void Awake()
    {
        // Auto-find the character creation controller if not set
        if (characterCreationController == null)
        {
            characterCreationController = FindObjectOfType<CharacterCreationController>();
        }
    }
}