using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;
    //[SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        if (musicSlider != null)
        {
            musicSlider.value = AudioManager.Instance.GetMusicVolume();
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (effectsSlider != null)
        {
            effectsSlider.value = AudioManager.Instance.GetEffectsVolume();
            effectsSlider.onValueChanged.AddListener(OnEffectsVolumeChanged);
        }

        //if (settingsPanel != null)
        //    settingsPanel.SetActive(false);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnEffectsVolumeChanged(float value)
    {
        AudioManager.Instance.SetEffectsVolume(value);
        AudioManager.Instance.SaveSettings();
    }

    //public void ToggleSettings()
    //{
    //    if (settingsPanel != null)
    //    {
    //        bool isOpen = !settingsPanel.activeSelf;
    //        settingsPanel.SetActive(isOpen);
    //    }
    //}
}