using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameTime : MonoBehaviour
{
    public static GameTime Instance { get; private set; }

    /// <summary>
    /// Fired after totalDays increases.
    /// GroundManager uses this to dry and age soil.
    /// </summary>
    public static event Action<int> DayChanged;

    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    public enum DayOfWeek
    {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    [Header("Current Time")]
    public int seconds = 0;
    public int minutes = 0;
    public int hours = 6;
    public int totalDays = 1;

    [Header("Time Settings")]
    [Tooltip(
        "96 means one full 24-hour in-game day " +
        "takes 15 real-life minutes.")]
    [SerializeField] private float gameSecondsPerRealSecond = 96f;

    [SerializeField] private int seasonLengthInDays = 28;

    [Header("Saving")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveEveryRealSeconds = 30f;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text timeText;

    private float timeAccumulator;
    private float autoSaveTimer;

    public DayOfWeek CurrentDayOfWeek
    {
        get
        {
            return (DayOfWeek)((totalDays - 1) % 7);
        }
    }

    public Season CurrentSeason
    {
        get
        {
            int safeSeasonLength =
                Mathf.Max(1, seasonLengthInDays);

            int seasonIndex =
                ((totalDays - 1) / safeSeasonLength) % 4;

            return (Season)seasonIndex;
        }
    }

    public TimeOfDay CurrentTimeOfDay
    {
        get
        {
            if (hours >= 5 && hours < 12)
                return TimeOfDay.Morning;

            if (hours >= 12 && hours < 17)
                return TimeOfDay.Afternoon;

            if (hours >= 17 && hours < 21)
                return TimeOfDay.Evening;

            return TimeOfDay.Night;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadTime();
        UpdateTimeUI();
    }

    private void Update()
    {
        AdvanceTime();

        if (autoSave)
            HandleAutoSave();

        // Temporary manual-save test key.
        if (Keyboard.current != null &&
            Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveCurrentTime();
        }
    }

    private void AdvanceTime()
    {
        timeAccumulator +=
            Time.deltaTime * gameSecondsPerRealSecond;

        while (timeAccumulator >= 1f)
        {
            timeAccumulator -= 1f;
            AddSecond();
        }
    }

    private void AddSecond()
    {
        seconds++;

        if (seconds >= 60)
        {
            seconds = 0;
            minutes++;
        }

        if (minutes >= 60)
        {
            minutes = 0;
            hours++;
        }

        if (hours >= 24)
        {
            hours = 0;
            totalDays++;

            DayChanged?.Invoke(totalDays);

            // Save immediately at the start of each new day.
            SaveCurrentTime();
        }

        UpdateTimeUI();
    }

    private void HandleAutoSave()
    {
        autoSaveTimer += Time.deltaTime;

        if (autoSaveTimer >= autoSaveEveryRealSeconds)
        {
            autoSaveTimer = 0f;
            SaveCurrentTime();
        }
    }

    public void SaveCurrentTime()
    {
        SaveManager.SaveTime(
            seconds,
            minutes,
            hours,
            totalDays);
    }

    private void LoadTime()
    {
        GameSaveData saveData =
            SaveManager.LoadGame();

        seconds = Mathf.Clamp(
            saveData.seconds,
            0,
            59);

        minutes = Mathf.Clamp(
            saveData.minutes,
            0,
            59);

        hours = Mathf.Clamp(
            saveData.hours,
            0,
            23);

        totalDays = Mathf.Max(
            1,
            saveData.totalDays);
    }

    private void OnApplicationQuit()
    {
        if (Instance == this)
            SaveCurrentTime();
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        SaveCurrentTime();
        Instance = null;
    }

    private void UpdateTimeUI()
    {
        if (timeText == null)
            return;

        timeText.text =
            CurrentDayOfWeek + "\n" +
            CurrentSeason + " - Day " + totalDays + "\n" +
            FormatClockTime() + "\n" +
            CurrentTimeOfDay;
    }

    private string FormatClockTime()
    {
        int displayHour = hours % 12;

        if (displayHour == 0)
            displayHour = 12;

        string amPm =
            hours < 12 ? "AM" : "PM";

        return displayHour.ToString("00") + ":" +
               minutes.ToString("00") + ":" +
               seconds.ToString("00") + " " +
               amPm;
    }
}
