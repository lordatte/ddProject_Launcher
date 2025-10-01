using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Button openButton;
    public Button closeButton;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.I;

    private bool isInventoryOpen = false;
    private PlayFabInventoryManager playFabInventory;

    void Start()
    {
        // Get reference to PlayFabInventoryManager
        playFabInventory = FindObjectOfType<PlayFabInventoryManager>();
        if (playFabInventory == null)
        {
            Debug.LogError("PlayFabInventoryManager not found in scene!");
        }

        // Ensure inventory is closed at start
        CloseInventory();

        // Setup button listeners
        if (openButton != null)
            openButton.onClick.AddListener(OpenInventory);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);
    }

    void Update()
    {
        // Toggle inventory with I key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // Refresh inventory UI when opening
            if (playFabInventory != null)
            {
                if (playFabInventory.IsInventoryLoaded())
                {
                    playFabInventory.RefreshInventoryUI();
                }
                else
                {
                    playFabInventory.LoadCharacterInventory();
                }
            }
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;

            // Clear inventory UI when closing
            if (playFabInventory != null)
            {
                playFabInventory.ClearInventoryUI();
            }
        }
    }

    // Public method to force refresh inventory (can be called from other scripts)
    public void RefreshInventory()
    {
        if (playFabInventory != null && isInventoryOpen)
        {
            playFabInventory.RefreshInventoryUI();
        }
    }
}