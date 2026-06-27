using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "save_slot_1.json";
    private const string BackupFileName = "save_slot_1.backup.json";
    private const string TemporaryFileName = "save_slot_1.temp.json";

    private static GameSaveData cachedData;

    private static string SavePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                SaveFileName);
        }
    }

    private static string BackupPath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                BackupFileName);
        }
    }

    private static string TemporaryPath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                TemporaryFileName);
        }
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void NewGame()
    {
        cachedData = new GameSaveData();
        SaveGame(cachedData);
    }

    public static GameSaveData LoadGame()
    {
        if (cachedData != null)
        {
            cachedData.EnsureValidCollections();
            return cachedData;
        }

        if (!HasSave())
        {
            Debug.LogWarning(
                "No save file found. Using default save data.");

            cachedData = new GameSaveData();
            return cachedData;
        }

        cachedData = TryLoadFile(SavePath);

        if (cachedData == null && File.Exists(BackupPath))
        {
            Debug.LogWarning(
                "Main save could not be loaded. Trying backup.");

            cachedData = TryLoadFile(BackupPath);
        }

        if (cachedData == null)
        {
            Debug.LogError(
                "No valid save could be loaded. " +
                "Using default save data.");

            cachedData = new GameSaveData();
        }

        cachedData.EnsureValidCollections();
        return cachedData;
    }

    public static void SaveGame(GameSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError(
                "SaveGame received null save data.");
            return;
        }

        try
        {
            saveData.EnsureValidCollections();
            cachedData = saveData;

            string json =
                JsonUtility.ToJson(saveData, true);

            File.WriteAllText(TemporaryPath, json);

            if (File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);

            File.Copy(TemporaryPath, SavePath, true);
            File.Delete(TemporaryPath);

            Debug.Log("Game saved to: " + SavePath);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Failed to save game: " +
                exception.Message);
        }
    }

    public static void SaveTime(
        int seconds,
        int minutes,
        int hours,
        int totalDays)
    {
        GameSaveData saveData = LoadGame();

        saveData.seconds = Mathf.Clamp(seconds, 0, 59);
        saveData.minutes = Mathf.Clamp(minutes, 0, 59);
        saveData.hours = Mathf.Clamp(hours, 0, 23);
        saveData.totalDays = Mathf.Max(1, totalDays);

        SaveGame(saveData);
    }

    public static void SaveGroundChanges(
        List<GroundCellSaveData> groundChanges)
    {
        GameSaveData saveData = LoadGame();

        saveData.groundChanges =
            groundChanges ??
            new List<GroundCellSaveData>();

        SaveGame(saveData);
    }

    public static void DeleteSave()
    {
        cachedData = null;

        DeleteIfExists(SavePath);
        DeleteIfExists(BackupPath);
        DeleteIfExists(TemporaryPath);

        Debug.Log("Deleted save files.");
    }

    public static string GetSavePathForDebug()
    {
        return SavePath;
    }

    private static GameSaveData TryLoadFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            GameSaveData loadedData =
                JsonUtility.FromJson<GameSaveData>(json);

            if (loadedData != null)
                loadedData.EnsureValidCollections();

            return loadedData;
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Failed to load save file '" +
                path + "': " + exception.Message);

            return null;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
