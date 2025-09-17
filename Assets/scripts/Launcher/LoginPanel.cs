using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject recoveryPanel;

    [Header("Button References")]
    public Button loginButton;
    public Button recoveryButton;
    public Button createAccountButton;

    void Start()
    {
        // Add button listeners
        if (recoveryButton != null)
            recoveryButton.onClick.AddListener(ShowRecoveryPanel);

        if (createAccountButton != null)
            createAccountButton.onClick.AddListener(ShowRegisterPanel);
    }

    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        recoveryPanel.SetActive(false);
    }

    public void ShowRecoveryPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        recoveryPanel.SetActive(true);
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        recoveryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (recoveryButton != null)
            recoveryButton.onClick.RemoveAllListeners();

        if (createAccountButton != null)
            createAccountButton.onClick.RemoveAllListeners();
    }
}