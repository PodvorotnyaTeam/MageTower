using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Settings")]
    [SerializeField] private AudioSettings audioSettings;

    private float effectsVolume = 0.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();

        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void LoadSettings()
    {
        if (audioSettings == null)
        {
            return;
        }

        audioSettings.LoadFromFile();

        if (musicSource != null)
            musicSource.volume = audioSettings.GetVolume("Music");

        effectsVolume = audioSettings.GetVolume("Effects");
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;

        audioSettings.SetVolume("Music", volume);
        audioSettings.SaveToFile();
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = volume;
        audioSettings.SetVolume("Effects", volume);
        audioSettings.SaveToFile();
    }

    public float GetMusicVolume() => audioSettings?.GetVolume("Music") ?? 0.8f;
    public float GetEffectsVolume() => effectsVolume;

    public void SaveSettings()
    {
        if (audioSettings != null)
            audioSettings.SaveToFile();
    }
}