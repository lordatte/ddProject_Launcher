using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Linq;

public class PlayFabInventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private bool loadInventoryOnStart = true;
    [SerializeField] private bool autoSaveLayout = true;

    [Header("UI References")]
    [SerializeField] private Transform inventorySlotsParent;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private CanvasGroup inventoryCanvasGroup;

    [Header("Item Database")]
    [SerializeField] private List<Item> itemDatabase = new List<Item>();

    private string inventoryCharacterId;
    private List<ItemInstance> characterInventory = new List<ItemInstance>();
    private bool isInventoryLoaded = false;
    private bool isUIPopulated = false;
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();

    // Dictionary for fast lookup by PlayFabId
    private Dictionary<string, Item> itemLookup = new Dictionary<string, Item>();

    // Inventory Layout System
    [System.Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public int quantity;
        public int slotIndex;
    }

    [System.Serializable]
    public class InventoryLayoutData
    {
        public List<InventorySlotData> slotData = new List<InventorySlotData>();
        public string characterId;
        public string timestamp;
    }

    private InventoryLayoutData currentLayout = new InventoryLayoutData();
    private const string INVENTORY_LAYOUT_KEY = "InventoryLayout";
    private float lastSaveTime;
    private const float SAVE_INTERVAL = 10f;

    // Prevent multiple rapid loads
    private bool isLoading = false;
    private float lastLoadTime = 0f;
    private const float LOAD_COOLDOWN = 1f;

    void Start()
    {
        InitializeItemLookup();
        InitializeInventorySlots();
        
        // Hide inventory until items are loaded
        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = 0f;
            inventoryCanvasGroup.blocksRaycasts = false;
        }

        if (loadInventoryOnStart)
        {
            LoadCharacterInventory();
        }
    }

    private void Update()
    {
        // Periodic auto-save if enabled
        if (autoSaveLayout && isInventoryLoaded && Time.time - lastSaveTime > SAVE_INTERVAL)
        {
            SaveInventoryLayout();
            lastSaveTime = Time.time;
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

    #region Inventory Layout System
    public void SaveInventoryLayout()
    {
        if (!isInventoryLoaded || string.IsNullOrEmpty(inventoryCharacterId) || !isUIPopulated)
        {
            Debug.LogWarning("Cannot save layout - inventory not fully loaded");
            return;
        }

        currentLayout.slotData.Clear();
        currentLayout.characterId = inventoryCharacterId;
        currentLayout.timestamp = System.DateTime.UtcNow.ToString();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];
            if (!slot.IsEmpty && slot.CurrentItem != null)
            {
                currentLayout.slotData.Add(new InventorySlotData
                {
                    itemId = slot.CurrentItem.PlayFabId,
                    quantity = slot.StackCount,
                    slotIndex = i
                });
            }
        }

        Debug.Log($"Saving inventory layout with {currentLayout.slotData.Count} items");

        // Save to PlayFab player data
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { INVENTORY_LAYOUT_KEY, JsonUtility.ToJson(currentLayout) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Inventory layout saved successfully to PlayFab"),
            error => Debug.LogError($"Failed to save inventory layout: {error.ErrorMessage}")
        );

        // Also save locally as backup
        SaveInventoryLayoutLocal();
    }

    public void LoadInventoryLayout()
    {
        if (string.IsNullOrEmpty(inventoryCharacterId))
        {
            Debug.LogWarning("Character ID not set, using default grouping");
            ApplyDefaultGrouping();
            return;
        }

        var request = new GetUserDataRequest
        {
            Keys = new List<string> { INVENTORY_LAYOUT_KEY }
        };

        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey(INVENTORY_LAYOUT_KEY))
            {
                string layoutJson = result.Data[INVENTORY_LAYOUT_KEY].Value;
                currentLayout = JsonUtility.FromJson<InventoryLayoutData>(layoutJson);

                if (currentLayout.characterId == inventoryCharacterId)
                {
                    Debug.Log("Loading saved inventory layout from PlayFab");
                    ApplySavedLayout();
                    return;
                }
                else
                {
                    Debug.Log("Character ID mismatch, using default grouping");
                }
            }
            else
            {
                Debug.Log("No saved layout found in PlayFab, checking local storage");
            }

            // If no saved layout in PlayFab, try local storage
            LoadInventoryLayoutLocal();

        }, error =>
        {
            Debug.LogWarning("Failed to load layout from PlayFab, trying local storage");
            LoadInventoryLayoutLocal();
        });
    }

    private void ApplySavedLayout()
    {
        ClearInventoryUI();

        // Create a dictionary of available items from character inventory
        Dictionary<string, int> availableItems = new Dictionary<string, int>();
        foreach (var itemInstance in characterInventory)
        {
            string itemId = itemInstance.ItemId;
            int quantity = itemInstance.RemainingUses ?? 1;

            if (availableItems.ContainsKey(itemId))
                availableItems[itemId] += quantity;
            else
                availableItems[itemId] = quantity;
        }

        // Apply the saved layout
        bool layoutAppliedSuccessfully = false;
        foreach (var slotData in currentLayout.slotData)
        {
            if (slotData.slotIndex < inventorySlots.Count &&
                availableItems.ContainsKey(slotData.itemId) &&
                availableItems[slotData.itemId] >= slotData.quantity)
            {
                Item itemData = GetItemData(slotData.itemId);
                if (itemData != null)
                {
                    if (inventorySlots[slotData.slotIndex].TryAddItem(itemData, slotData.quantity))
                    {
                        availableItems[slotData.itemId] -= slotData.quantity;
                        if (availableItems[slotData.itemId] <= 0)
                            availableItems.Remove(slotData.itemId);
                        layoutAppliedSuccessfully = true;
                    }
                }
            }
        }

        // Add any remaining items that weren't in the saved layout
        if (availableItems.Count > 0)
        {
            AddRemainingItems(availableItems);
        }

        // Show inventory immediately after applying layout
        ShowInventoryUI();
        isUIPopulated = true;

        Debug.Log("Applied saved inventory layout");
    }

    private void ApplyDefaultGrouping()
    {
        ClearInventoryUI();
        PopulateInventoryUI();
        Debug.Log("Applied default inventory grouping");
    }

    private void AddRemainingItems(Dictionary<string, int> remainingItems)
    {
        if (remainingItems.Count == 0) return;

        int currentSlot = 0;
        foreach (var itemEntry in remainingItems)
        {
            string itemId = itemEntry.Key;
            int totalCount = itemEntry.Value;
            Item itemData = GetItemData(itemId);

            if (itemData == null) continue;

            if (itemData.IsStackable)
            {
                while (totalCount > 0 && currentSlot < inventorySlots.Count)
                {
                    if (inventorySlots[currentSlot].IsEmpty)
                    {
                        int stackAmount = Mathf.Min(totalCount, itemData.MaxStackSize);
                        if (inventorySlots[currentSlot].TryAddItem(itemData, stackAmount))
                        {
                            totalCount -= stackAmount;
                        }
                    }
                    currentSlot++;
                }
            }
            else
            {
                for (int i = 0; i < totalCount && currentSlot < inventorySlots.Count; i++)
                {
                    while (currentSlot < inventorySlots.Count && !inventorySlots[currentSlot].IsEmpty)
                    {
                        currentSlot++;
                    }

                    if (currentSlot < inventorySlots.Count)
                    {
                        inventorySlots[currentSlot].TryAddItem(itemData, 1);
                        currentSlot++;
                    }
                }
            }
        }
    }

    // Local storage as backup
    private void SaveInventoryLayoutLocal()
    {
        string localKey = $"InventoryLayout_{inventoryCharacterId}";
        PlayerPrefs.SetString(localKey, JsonUtility.ToJson(currentLayout));
        PlayerPrefs.Save();
        Debug.Log("Inventory layout saved locally");
    }

    private void LoadInventoryLayoutLocal()
    {
        string localKey = $"InventoryLayout_{inventoryCharacterId}";
        string savedLayout = PlayerPrefs.GetString(localKey, "");

        if (!string.IsNullOrEmpty(savedLayout))
        {
            currentLayout = JsonUtility.FromJson<InventoryLayoutData>(savedLayout);
            if (currentLayout.characterId == inventoryCharacterId)
            {
                Debug.Log("Loading saved inventory layout from local storage");
                ApplySavedLayout();
                return;
            }
        }

        Debug.Log("No saved layout found, using default grouping");
        ApplyDefaultGrouping();
    }

    // Call this when inventory is closed or when drag/drop operations complete
    public void OnInventoryStateChanged()
    {
        if (autoSaveLayout && isUIPopulated)
        {
            // Debounced save - wait a frame to avoid multiple rapid saves
            CancelInvoke("DebouncedSave");
            Invoke("DebouncedSave", 0.1f);
        }
    }

    private void DebouncedSave()
    {
        SaveInventoryLayout();
    }

    private void ShowInventoryUI()
    {
        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = 1f;
            inventoryCanvasGroup.blocksRaycasts = true;
        }
    }

    public void HideInventoryUI()
    {
        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = 0f;
            inventoryCanvasGroup.blocksRaycasts = false;
        }
    }
    #endregion

    #region Public Methods
    public void LoadCharacterInventory()
    {
        // Prevent multiple rapid loads
        if (isLoading || (Time.time - lastLoadTime < LOAD_COOLDOWN && isInventoryLoaded))
        {
            Debug.Log("Inventory load skipped - too soon since last load");
            return;
        }

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

        isLoading = true;
        lastLoadTime = Time.time;

        // Hide UI while loading
        HideInventoryUI();

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
        LoadInventoryLayout(); // Use layout system instead of direct population
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
        isUIPopulated = false;
    }

    private void PopulateInventoryUI()
    {
        // Group items by PlayFab ItemId and calculate total stack count
        var itemGroups = characterInventory
            .GroupBy(item => item.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                TotalCount = group.Sum(item => item.RemainingUses ?? 1),
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
                        currentSlot++;
                    }

                    if (remainingCount <= 0) break;
                }
            }
            else
            {
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
                        currentSlot++;
                    }
                }
            }
        }

        // Show inventory immediately after populating
        ShowInventoryUI();
        isUIPopulated = true;

        Debug.Log($"Successfully populated {currentSlot} slots with inventory items");
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

    public void RemoveItemFromDatabase(Item item)
    {
        if (item != null)
        {
            itemDatabase.Remove(item);
            itemLookup.Remove(item.PlayFabId);
        }
    }

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

    public bool IsUIPopulated()
    {
        return isUIPopulated;
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
        isLoading = false;

        Debug.Log($"Successfully loaded {characterInventory.Count} items from character inventory.");

        // Load the saved layout instead of auto-grouping
        LoadInventoryLayout();
    }

    private void OnGetInventoryFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to load character inventory: {error.ErrorMessage}");
        isInventoryLoaded = false;
        isLoading = false;
        
        // Still show empty inventory on failure
        ShowInventoryUI();
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

    public int GetTotalItemCount(string playFabItemId)
    {
        return characterInventory
            .Where(item => item.ItemId == playFabItemId)
            .Sum(item => item.RemainingUses ?? 1);
    }
    #endregion

    // For testing purposes
    [ContextMenu("Reload Inventory")]
    public void ReloadInventory()
    {
        LoadCharacterInventory();
    }

    [ContextMenu("Save Layout Now")]
    public void ForceSaveLayout()
    {
        SaveInventoryLayout();
    }

    [ContextMenu("Clear Saved Layout")]
    public void ClearSavedLayout()
    {
        currentLayout = new InventoryLayoutData();
        string localKey = $"InventoryLayout_{inventoryCharacterId}";
        PlayerPrefs.DeleteKey(localKey);
        Debug.Log("Cleared saved layout");
    }

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

    // Public method to show/hide inventory with proper state management
    public void ToggleInventory()
    {
        if (!isInventoryLoaded)
        {
            LoadCharacterInventory();
        }
        else if (isUIPopulated)
        {
            if (inventoryCanvasGroup != null && inventoryCanvasGroup.alpha > 0)
            {
                HideInventoryUI();
            }
            else
            {
                ShowInventoryUI();
            }
        }
    }

    public void ShowInventory()
    {
        if (!isInventoryLoaded)
        {
            LoadCharacterInventory();
        }
        else if (isUIPopulated)
        {
            ShowInventoryUI();
        }
    }

    public void HideInventory()
    {
        HideInventoryUI();
        OnInventoryStateChanged(); // Save when hiding
    }
}