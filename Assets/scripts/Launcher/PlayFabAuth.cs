using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayFabAuth : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPassword;
    [SerializeField] private Button loginButton;

    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private TMP_InputField registerConfirmPassword;
    [SerializeField] private Button registerButton;

    [SerializeField] private TMP_InputField recoveryEmail;
    [SerializeField] private Button recoveryButton;

    [SerializeField] private TMP_Text errorText;

    [Header("UI Panel References")]
    [SerializeField] private GameObject authPanel; // Panel with login/register forms
    [SerializeField] private GameObject mainPanel; // Panel with slider and buttons (after login)

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        recoveryButton.onClick.AddListener(OnRecoveryClicked);

        errorText.text = "";

        // Check if already logged in (persistent login)
        CheckExistingLogin();
    }

    private void CheckExistingLogin()
    {
        // If already logged in, show main UI directly
        if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
        {
            ShowMainUI();
        }
        else
        {
            ShowAuthUI();
        }
    }

    private void OnLoginClicked()
    {
        string email = loginEmail.text;
        string password = loginPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowErrorMessage("Please enter both email and password");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address");
            return;
        }

        LoginWithEmail(email, password);
    }

    private void LoginWithEmail(string email, string password)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        ShowSuccessMessage("Login successful!");
        Debug.Log("Login successful");

        // Set the logged in account in AccountManager
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.SetLoggedInAccount(loginEmail.text);
        }



        ShowMainUI();
    }

    private void ShowMainUI()
    {
        // Show main panel and hide auth panel
        if (authPanel != null) authPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void ShowAuthUI()
    {
        // Show auth panel and hide main panel
        if (authPanel != null) authPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    private void OnLoginFailure(PlayFabError error)
    {
        string errorMessage = "Login failed: ";

        switch (error.Error)
        {
            case PlayFabErrorCode.AccountNotFound:
                errorMessage += "Account not found. Please register first.";
                break;
            case PlayFabErrorCode.InvalidEmailOrPassword:
                errorMessage += "Invalid email or password.";
                break;
            case PlayFabErrorCode.InvalidParams:
                errorMessage += "Invalid parameters provided.";
                break;
            case PlayFabErrorCode.AccountBanned:
                errorMessage += "Account is banned.";
                break;
            case PlayFabErrorCode.AccountDeleted:
                errorMessage += "Account has been deleted.";
                break;
            default:
                errorMessage += "An unexpected error occurred. Please try again.";
                break;
        }

        ShowErrorMessage(errorMessage);
    }

    // Add a logout method
    public void Logout()
    {
        // PlayFab logout
        PlayFabClientAPI.ForgetAllCredentials();

        // Clear account from AccountManager
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.ClearAccount();
        }

        // Return to launcher scene through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToLauncher();
        }

        // Show auth UI
        ShowAuthUI();

        // Clear input fields
        loginEmail.text = "";
        loginPassword.text = "";

        Debug.Log("Logged out successfully");
    }

    private void OnRegisterClicked()
    {
        string email = registerEmail.text;
        string password = registerPassword.text;
        string confirmPassword = registerConfirmPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowErrorMessage("Please fill all registration fields");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address");
            return;
        }

        if (password != confirmPassword)
        {
            ShowErrorMessage("Passwords do not match");
            return;
        }

        if (password.Length < 6)
        {
            ShowErrorMessage("Password must be at least 6 characters long");
            return;
        }

        RegisterWithEmail(email, password);
    }

    private void RegisterWithEmail(string email, string password)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterFailure);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        ShowSuccessMessage("Registration successful!");

        // Automatically log in after successful registration
        LoginWithEmail(registerEmail.text, registerPassword.text);

        registerEmail.text = "";
        registerPassword.text = "";
        registerConfirmPassword.text = "";
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        string errorMessage = "Registration failed: ";

        switch (error.Error)
        {
            case PlayFabErrorCode.EmailAddressNotAvailable:
                errorMessage += "Email already registered.";
                break;
            case PlayFabErrorCode.InvalidEmailAddress:
                errorMessage += "Invalid email format.";
                break;
            case PlayFabErrorCode.InvalidPassword:
                errorMessage += "Invalid password. It must be at least 6 characters.";
                break;
            case PlayFabErrorCode.UsernameNotAvailable:
                errorMessage += "Username not available.";
                break;
            default:
                errorMessage += "An unexpected error occurred. Please try again.";
                break;
        }

        ShowErrorMessage(errorMessage);
    }

    private void OnRecoveryClicked()
    {
        string email = recoveryEmail.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowErrorMessage("Please enter your email address");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address");
            return;
        }

        SendPasswordResetEmail(email);
    }

    private void SendPasswordResetEmail(string email)
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = PlayFabSettings.TitleId
        };

        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnRecoverySuccess, OnRecoveryFailure);
    }

    private void OnRecoverySuccess(SendAccountRecoveryEmailResult result)
    {
        ShowSuccessMessage("Password reset email sent! Please check your inbox.");
        recoveryEmail.text = "";
    }

    private void OnRecoveryFailure(PlayFabError error)
    {
        string errorMessage = "Password recovery failed: ";

        switch (error.Error)
        {
            case PlayFabErrorCode.AccountNotFound:
                errorMessage += "No account found with this email address.";
                break;
            case PlayFabErrorCode.InvalidEmailAddress:
                errorMessage += "Invalid email format.";
                break;
            default:
                errorMessage += "An unexpected error occurred. Please try again.";
                break;
        }

        ShowErrorMessage(errorMessage);
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(email, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void ShowSuccessMessage(string message)
    {
        errorText.text = message;
        errorText.color = Color.green;
        CancelInvoke("ClearErrorMessage");
        Invoke("ClearErrorMessage", 5f);
    }

    private void ShowErrorMessage(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        CancelInvoke("ClearErrorMessage");
        Invoke("ClearErrorMessage", 5f);
    }

    private void ClearErrorMessage()
    {
        errorText.text = "";
    }
}