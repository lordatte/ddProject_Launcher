using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Linq;

public class PlayFabInventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private bool loadInventoryOnStart = true;

    [Header("UI References")]
    [SerializeField] private Transform inventorySlotsParent;
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Item Database")]
    [SerializeField] private List<Item> itemDatabase = new List<Item>();

    private string inventoryCharacterId;
    private List<ItemInstance> characterInventory = new List<ItemInstance>();
    private bool isInventoryLoaded = false;
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();

    // Dictionary for fast lookup by PlayFabId
    private Dictionary<string, Item> itemLookup = new Dictionary<string, Item>();

    void Start()
    {
        InitializeItemLookup();
        InitializeInventorySlots();

        if (loadInventoryOnStart)
        {
            LoadCharacterInventory();
        }
    }

    private void InitializeItemLookup()
    {
        itemLookup.Clear();

        foreach (Item item in itemDatabase)
        {
            if (item != null && !string.IsNullOrEmpty(item.PlayFabId))
            {
                if (!itemLookup.ContainsKey(item.PlayFabId))
                {
                    itemLookup[item.PlayFabId] = item;
                }
                else
                {
                    Debug.LogWarning($"Duplicate PlayFabId found: {item.PlayFabId} for item {item.ItemName}");
                }
            }
            else if (item != null)
            {
                Debug.LogWarning($"Item {item.ItemName} has no PlayFabId assigned!");
            }
        }

        Debug.Log($"Initialized item lookup with {itemLookup.Count} items");
    }

    private void InitializeInventorySlots()
    {
        if (inventorySlotsParent == null || inventorySlotPrefab == null)
        {
            Debug.LogWarning("Inventory UI references not set. UI will not be updated.");
            return;
        }

        // Clear existing slots
        foreach (Transform child in inventorySlotsParent)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        // Create 45 inventory slots
        for (int i = 0; i < 45; i++)
        {
            GameObject slotObject = Instantiate(inventorySlotPrefab, inventorySlotsParent);
            InventorySlot slot = slotObject.GetComponent<InventorySlot>();

            if (slot != null)
            {
                inventorySlots.Add(slot);
            }
            else
            {
                Debug.LogError("InventorySlot component not found on prefab!");
            }
        }
    }

    #region Public Methods
    public void LoadCharacterInventory()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("PlayFab client is not logged in. Cannot load inventory.");
            return;
        }

        if (CharacterManager.Instance == null || string.IsNullOrEmpty(CharacterManager.Instance.CharacterId))
        {
            Debug.LogWarning("CharacterManager not available or character ID not set. Cannot load character inventory.");
            return;
        }

        inventoryCharacterId = CharacterManager.Instance.CharacterId;

        var request = new GetCharacterInventoryRequest
        {
            CharacterId = inventoryCharacterId
        };
        PlayFabClientAPI.GetCharacterInventory(request, OnGetInventorySuccess, OnGetInventoryFailure);
    }

    public void RefreshInventoryUI()
    {
        if (!isInventoryLoaded)
        {
            Debug.LogWarning("Inventory not loaded yet. Cannot refresh UI.");
            return;
        }

        ClearInventoryUI();
        PopulateInventoryUI();
    }

    public void ClearInventoryUI()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                slot.RemoveItem();
            }
        }
    }

    private void PopulateInventoryUI()
    {
        // Group items by PlayFab ItemId and calculate total stack count
        var itemGroups = characterInventory
            .GroupBy(item => item.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                TotalCount = group.Sum(item => item.RemainingUses ?? 1), // Use RemainingUses for stack count
                ItemInstances = group.ToList()
            })
            .ToList();

        Debug.Log($"Processing {itemGroups.Count} item groups from PlayFab inventory");

        int currentSlot = 0;

        foreach (var itemGroup in itemGroups)
        {
            if (currentSlot >= inventorySlots.Count)
            {
                Debug.LogWarning("Not enough inventory slots to display all items!");
                break;
            }

            Item itemData = GetItemData(itemGroup.ItemId);
            if (itemData == null)
            {
                Debug.LogWarning($"No Item data found for PlayFab ItemId: {itemGroup.ItemId}");
                continue;
            }

            Debug.Log($"Processing item: {itemData.ItemName}, TotalCount: {itemGroup.TotalCount}, IsStackable: {itemData.IsStackable}");

            if (itemData.IsStackable)
            {
                // For stackable items, distribute across slots if needed
                int remainingCount = itemGroup.TotalCount;

                while (remainingCount > 0 && currentSlot < inventorySlots.Count)
                {
                    InventorySlot slot = inventorySlots[currentSlot];
                    int stackAmount = Mathf.Min(remainingCount, itemData.MaxStackSize);

                    if (slot.TryAddItem(itemData, stackAmount))
                    {
                        Debug.Log($"Added {stackAmount} of {itemData.ItemName} to slot {currentSlot}");
                        remainingCount -= stackAmount;
                        currentSlot++;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to add item to slot {currentSlot}");
                        currentSlot++; // Move to next slot even if failed
                    }

                    if (remainingCount <= 0) break;
                }
            }
            else
            {
                // For non-stackable items, put each instance in separate slots
                foreach (var itemInstance in itemGroup.ItemInstances)
                {
                    if (currentSlot >= inventorySlots.Count) break;

                    InventorySlot slot = inventorySlots[currentSlot];
                    int instanceCount = itemInstance.RemainingUses ?? 1;

                    if (slot.TryAddItem(itemData, instanceCount))
                    {
                        Debug.Log($"Added non-stackable item {itemData.ItemName} to slot {currentSlot} with count {instanceCount}");
                        currentSlot++;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to add non-stackable item to slot {currentSlot}");
                        currentSlot++; // Move to next slot even if failed
                    }
                }
            }
        }

        Debug.Log($"Successfully populated {currentSlot} slots with inventory items");

        // Log remaining empty slots
        int emptySlots = inventorySlots.Count - currentSlot;
        Debug.Log($"{emptySlots} empty slots remaining");
    }

    private Item GetItemData(string playFabItemId)
    {
        if (itemLookup.ContainsKey(playFabItemId))
        {
            return itemLookup[playFabItemId];
        }

        Debug.LogWarning($"Item with PlayFab ID {playFabItemId} not found in database");
        return null;
    }

    // Method to add items to the database at runtime
    public void AddItemToDatabase(Item item)
    {
        if (item != null && !string.IsNullOrEmpty(item.PlayFabId))
        {
            if (!itemDatabase.Contains(item))
            {
                itemDatabase.Add(item);
            }

            if (!itemLookup.ContainsKey(item.PlayFabId))
            {
                itemLookup[item.PlayFabId] = item;
            }
        }
    }

    // Method to remove items from the database at runtime
    public void RemoveItemFromDatabase(Item item)
    {
        if (item != null)
        {
            itemDatabase.Remove(item);
            itemLookup.Remove(item.PlayFabId);
        }
    }

    // Method to clear and rebuild the lookup (call this if you modify the itemDatabase list in inspector)
    public void RebuildItemLookup()
    {
        InitializeItemLookup();
    }

    public List<ItemInstance> GetCharacterInventory()
    {
        return characterInventory;
    }

    public bool IsInventoryLoaded()
    {
        return isInventoryLoaded;
    }

    public void PrintInventoryToConsole()
    {
        if (!isInventoryLoaded)
        {
            Debug.Log("Inventory not loaded yet. Call LoadCharacterInventory() first.");
            return;
        }

        if (characterInventory.Count == 0)
        {
            Debug.Log("Character inventory is empty.");
            return;
        }

        Debug.Log("=== CHARACTER INVENTORY ===");
        for (int i = 0; i < characterInventory.Count; i++)
        {
            string itemId = characterInventory[i].ItemId;
            string itemName = characterInventory[i].DisplayName ?? "No Name";
            int remainingUses = characterInventory[i].RemainingUses ?? 1;

            Debug.Log($"Inventory Slot {i + 1}: {itemId} | Name: {itemName} | Uses: {remainingUses}");
        }
        Debug.Log("===========================");
    }

    public string GetItemIdBySlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < characterInventory.Count)
        {
            return characterInventory[slotIndex].ItemId;
        }
        return null;
    }

    public ItemInstance GetItemBySlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < characterInventory.Count)
        {
            return characterInventory[slotIndex];
        }
        return null;
    }
    #endregion

    #region PlayFab Callbacks
    private void OnGetInventorySuccess(GetCharacterInventoryResult result)
    {
        characterInventory = result.Inventory ?? new List<ItemInstance>();
        isInventoryLoaded = true;

        Debug.Log($"Successfully loaded {characterInventory.Count} items from character inventory.");

        // Log detailed information about each item
        foreach (var item in characterInventory)
        {
            Debug.Log($"Item: {item.ItemId}, DisplayName: {item.DisplayName}, RemainingUses: {item.RemainingUses}");
        }

        PrintInventoryToConsole();

        // Auto-refresh UI when inventory is loaded
        RefreshInventoryUI();
    }

    private void OnGetInventoryFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to load character inventory: {error.ErrorMessage}");
        isInventoryLoaded = false;
    }
    #endregion

    #region Utility Methods
    public int GetInventoryItemCount()
    {
        return characterInventory.Count;
    }

    public bool HasItem(string playFabItemId)
    {
        return characterInventory.Any(item => item.ItemId == playFabItemId);
    }

    public int GetItemCount(string playFabItemId)
    {
        return characterInventory.Count(item => item.ItemId == playFabItemId);
    }

    // Get total count including stack sizes
    public int GetTotalItemCount(string playFabItemId)
    {
        return characterInventory
            .Where(item => item.ItemId == playFabItemId)
            .Sum(item => item.RemainingUses ?? 1);
    }
    #endregion

    // For testing purposes - you can call this from a button or other input
    [ContextMenu("Reload Inventory")]
    private void ReloadInventory()
    {
        LoadCharacterInventory();
    }

    // Editor method to help with debugging
    [ContextMenu("Print Item Database")]
    private void PrintItemDatabase()
    {
        Debug.Log("=== ITEM DATABASE ===");
        foreach (var item in itemDatabase)
        {
            if (item != null)
            {
                Debug.Log($"Item: {item.ItemName} | PlayFabId: {item.PlayFabId} | Stackable: {item.IsStackable} | MaxStack: {item.MaxStackSize}");
            }
        }
        Debug.Log("=====================");
    }
}