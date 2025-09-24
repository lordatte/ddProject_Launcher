using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryOpensClose : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private UnityEngine.UI.Button openButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    // Start is called before the first frame update
    void Start()
    {
        // Ensure inventory is closed at start
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // Set up button click events
        if (openButton != null)
            openButton.onClick.AddListener(OpenInventory);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);
    }

    // Update is called once per frame
    void Update()
    {
        // Check for "I" key press to toggle inventory
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    private void OpenInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    private void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void ToggleInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    // Clean up event listeners when the script is destroyed
    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenInventory);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseInventory);
    }
}