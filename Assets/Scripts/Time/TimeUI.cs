using TMPro;
using UnityEngine;

public class TimeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

    private void Start()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError("TimeUI: TimeManager.Instance is null!");
            return;
        }

        TimeManager.Instance.OnTimeChanged += OnTimeChanged;
        TimeManager.Instance.OnDayStarted += OnDayStarted;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance == null)
            return;

        TimeManager.Instance.OnTimeChanged -= OnTimeChanged;
        TimeManager.Instance.OnDayStarted -= OnDayStarted;
    }

    private void OnTimeChanged(int hour, int minute)
    {
        timeText.text = $"{hour:00}:{minute:00}";
    }

    private void OnDayStarted(int day)
    {
        dayText.text = $"Day {day}";
    }

    private void UpdateUI()
    {
        dayText.text = $"Day {TimeManager.Instance.CurrentDay}";
        timeText.text = TimeManager.Instance.GetTimeString();
    }
}