using UnityEngine;
using UnityEngine.UI;

public class RecoveryPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject recoveryPanel;
    
    [Header("Button References")]
    public Button recoverButton;
    public Button haveAccountButton;

    void Start()
    {
        // Add button listener
        if (haveAccountButton != null)
            haveAccountButton.onClick.AddListener(ShowLoginPanel);
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        recoveryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // Clean up listener
        if (haveAccountButton != null)
            haveAccountButton.onClick.RemoveAllListeners();
    }
}