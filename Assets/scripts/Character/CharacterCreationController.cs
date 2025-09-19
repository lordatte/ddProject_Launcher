using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class CharacterCreationController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Image maleIcon;
    [SerializeField] private Image maleChar;
    [SerializeField] private Image femaleIcon;
    [SerializeField] private Image femaleChar;
    [SerializeField] private TMP_InputField characterNameInput;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = Color.white;
    [SerializeField] private Vector3 selectedScale = new Vector3(1.4f, 1.4f, 1f);
    [SerializeField] private Vector3 unselectedScale = Vector3.one;

    [Header("Button Colors")]
    [SerializeField] private Color maleSelectedColor = new Color(0f, 184f / 255f, 1f); // 00B8FF
    [SerializeField] private Color maleUnselectedColor = new Color(25f / 255f, 91f / 255f, 109f / 255f); // 195B6D
    [SerializeField] private Color femaleSelectedColor = new Color(252f / 255f, 59f / 255f, 147f / 255f); // FC3B93
    [SerializeField] private Color femaleUnselectedColor = new Color(107f / 255f, 21f / 255f, 56f / 255f); // 6B1538

    private string selectedGender = "";

    private void Start()
    {
        // Initialize UI state
        ResetGenderSelection();

        // Add button listeners
        maleButton.onClick.AddListener(SelectMale);
        femaleButton.onClick.AddListener(SelectFemale);
        confirmBtn.onClick.AddListener(OnConfirmButtonClick);

        // Remove the real-time validation for input field
        characterNameInput.onValueChanged.RemoveAllListeners();

        // Enable confirm button by default
        confirmBtn.interactable = true;
        errorText.text = "";
    }

    private void SelectMale()
    {
        selectedGender = "male";

        // Set male visuals
        maleIcon.color = selectedColor;
        maleChar.color = selectedColor;
        maleChar.transform.localScale = selectedScale;
        maleButton.image.color = maleSelectedColor;

        // Reset female visuals
        femaleIcon.color = unselectedColor;
        femaleChar.color = unselectedColor;
        femaleChar.transform.localScale = unselectedScale;
        femaleButton.image.color = femaleUnselectedColor;

        // Clear any previous error when selecting gender
        errorText.text = "";
    }

    private void SelectFemale()
    {
        selectedGender = "female";

        // Set female visuals
        femaleIcon.color = selectedColor;
        femaleChar.color = selectedColor;
        femaleChar.transform.localScale = selectedScale;
        femaleButton.image.color = femaleSelectedColor;

        // Reset male visuals
        maleIcon.color = unselectedColor;
        maleChar.color = unselectedColor;
        maleChar.transform.localScale = unselectedScale;
        maleButton.image.color = maleUnselectedColor;

        // Clear any previous error when selecting gender
        errorText.text = "";
    }

    private void ResetGenderSelection()
    {
        maleIcon.color = unselectedColor;
        maleChar.color = unselectedColor;
        femaleIcon.color = unselectedColor;
        femaleChar.color = unselectedColor;
        maleChar.transform.localScale = unselectedScale;
        femaleChar.transform.localScale = unselectedScale;

        // Reset button colors
        maleButton.image.color = maleUnselectedColor;
        femaleButton.image.color = femaleUnselectedColor;

        selectedGender = "";
    }

    private bool IsValidName(string name)
    {
        if (name.Length < 4)
            return false;

        // Regex to check for only letters and numbers (no special characters)
        Regex regex = new Regex(@"^[a-zA-Z0-9\s]+$");
        return regex.IsMatch(name);
    }

    private void OnConfirmButtonClick()
    {
        string name = characterNameInput.text.Trim();

        if (IsValidName(name) && !string.IsNullOrEmpty(selectedGender))
        {
            // Success! Do something with the selected gender and name
            Debug.Log($"Character created: {name} ({selectedGender})");
            errorText.text = "Character created successfully!";

            // Here you would typically load the next scene or save the character data
        }
        else
        {
            if (string.IsNullOrEmpty(selectedGender) && string.IsNullOrEmpty(name))
            {
                errorText.text = "Please select a gender and enter a name (min 4 letters, no special characters)";
            }
            else if (string.IsNullOrEmpty(selectedGender))
            {
                errorText.text = "Please select a gender";
            }
            else if (!IsValidName(name))
            {
                errorText.text = "Name must be at least 4 letters with no special characters";
            }
        }
    }

    // Public method to get the selected data (optional)
    public (string gender, string name) GetCharacterData()
    {
        return (selectedGender, characterNameInput.text.Trim());
    }

    private void OnDestroy()
    {
        // Clean up listeners
        maleButton.onClick.RemoveListener(SelectMale);
        femaleButton.onClick.RemoveListener(SelectFemale);
        confirmBtn.onClick.RemoveListener(OnConfirmButtonClick);
    }
}