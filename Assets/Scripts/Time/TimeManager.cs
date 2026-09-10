using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Start Time")]
    [SerializeField] private int startDay = 1;
    [SerializeField] private int startHour = 8;
    [SerializeField] private int startMinute = 0;

    [Header("Day / Night")]
    [SerializeField] private int dayStartHour = 6;
    [SerializeField] private int nightStartHour = 22;

    [Header("Time Speed")]
    [Tooltip("Сколько реальных секунд занимает 1 игровая минута.")]
    [SerializeField] private float realSecondsPerGameMinute = 1f;

    private int currentDay;
    private int currentHour;
    private int currentMinute;

    private float timer;

    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;

    public bool IsDay => currentHour >= dayStartHour && currentHour < nightStartHour;

    public bool IsNight => !IsDay;

    public event Action<int, int> OnTimeChanged;
    public event Action<int> OnDayStarted;
    public event Action OnNightStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentDay = startDay;
        currentHour = startHour;
        currentMinute = startMinute;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= realSecondsPerGameMinute)
        {
            timer -= realSecondsPerGameMinute;

            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        currentMinute++;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            if (currentHour >= 24)
            {
                currentHour = 0;
                currentDay++;

                OnDayStarted?.Invoke(currentDay);
            }
        }

        OnTimeChanged?.Invoke(currentHour, currentMinute);

        if (currentHour == nightStartHour && currentMinute == 0)
        {
            OnNightStarted?.Invoke();
        }

    }

    public string GetTimeString()
    {
        return $"{currentHour:00}:{currentMinute:00}";
    }
}