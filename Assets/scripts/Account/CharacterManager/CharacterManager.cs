using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [SerializeField] private string characterName;
    [SerializeField] private string characterId;
    [SerializeField] private string characterType;
    [SerializeField] private string playFabId;

    // Public properties to access private fields
    public string CharacterName => characterName;
    public string CharacterId => characterId;
    public string CharacterType => characterType;
    public string PlayFabId => playFabId;

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
        if (CheckCharacters.Instance != null)
        {
            CheckCharacters.Instance.OnCharactersChecked += HandleCharactersChecked;
        }
    }

    private void OnDestroy()
    {
        if (CheckCharacters.Instance != null)
        {
            CheckCharacters.Instance.OnCharactersChecked -= HandleCharactersChecked;
        }
    }

    private void HandleCharactersChecked(bool hasCharacters)
    {
        if (hasCharacters)
        {
            FillCharacterData();
        }
        else
        {
            Debug.Log("no character found");
        }
    }

    public void FillCharacterData()
    {
        var characterList = CheckCharacters.Instance.GetCharacterList();

        if (characterList != null && characterList.Count > 0)
        {
            foreach (var character in characterList)
            {
                characterId = character.CharacterId;
                characterType = character.CharacterType;
                characterName = character.CharacterName;
                playFabId = AccountManager.Instance.PlayFabId;

                Debug.Log(characterName);
            }
        }
    }
}