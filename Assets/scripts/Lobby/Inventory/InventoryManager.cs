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

    void Start()
    {
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

        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;
        }
    }
}