using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaveController : MonoBehaviour
{
    [Header("Loading")]
    [SerializeField] private bool loadSavedPositionOnStart = true;

    [Header("Saving")]
    [SerializeField] private bool savePosition = true;
    [SerializeField] private bool autoSave = true;

    [Min(1f)]
    [SerializeField] private float autoSaveEverySeconds = 5f;

    private float autoSaveTimer;

    private void Start()
    {
        if (loadSavedPositionOnStart)
            LoadSavedPosition();
    }

    private void Update()
    {
        if (!savePosition || !autoSave)
            return;

        autoSaveTimer += Time.deltaTime;

        if (autoSaveTimer >= autoSaveEverySeconds)
        {
            autoSaveTimer = 0f;
            SaveCurrentPosition();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (savePosition)
            SaveCurrentPosition();
    }

    private void OnApplicationQuit()
    {
        if (savePosition)
            SaveCurrentPosition();
    }

    public void SaveCurrentPosition()
    {
        string sceneName =
            SceneManager.GetActiveScene().name;

        SaveManager.SavePlayerState(
            transform.position,
            sceneName);
    }

    private void LoadSavedPosition()
    {
        GameSaveData saveData = SaveManager.LoadGame();

        if (!saveData.hasPlayerPosition)
            return;

        string activeScene =
            SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(saveData.currentScene) &&
            saveData.currentScene != activeScene)
        {
            return;
        }

        transform.position =
            new Vector3(
                saveData.playerX,
                saveData.playerY,
                saveData.playerZ);
    }
}