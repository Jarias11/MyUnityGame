using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string newGameSceneName = "GameScene";

    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text statusText;

    private bool isLoading;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "";

        SetupButtons();
        RefreshContinueButton();
    }

    private void SetupButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void RefreshContinueButton()
    {
        bool hasSave = SaveManager.HasSave();

        if (continueButton != null)
            continueButton.interactable = hasSave;
    }

    public void NewGame()
    {
        if (isLoading)
            return;

        SaveManager.NewGame();

        StartCoroutine(LoadSceneRoutine(newGameSceneName));
    }

    public void ContinueGame()
    {
        if (isLoading)
            return;

        if (!SaveManager.HasSave())
        {
            SetStatus("No save file found.");
            RefreshContinueButton();
            return;
        }

        StartCoroutine(LoadSceneRoutine(newGameSceneName));
    }

    public void OpenOptions()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        SetStatus("");
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (buttonPanel != null)
            buttonPanel.SetActive(true);

        SetStatus("");
    }

    public void QuitGame()
    {
        if (isLoading)
            return;

        SetStatus("Quitting...");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        SetStatus("Loading...");

        if (newGameButton != null) newGameButton.interactable = false;
        if (continueButton != null) continueButton.interactable = false;
        if (optionsButton != null) optionsButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}