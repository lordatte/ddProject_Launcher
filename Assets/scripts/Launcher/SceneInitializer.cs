using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject recoveryPanel;

    void Start()
    {
        InitializePanels();
    }

    void InitializePanels()
    {

        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        recoveryPanel.SetActive(false);

    }


}