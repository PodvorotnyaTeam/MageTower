using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "Game/AudioSettings")]
public class AudioSettings : ScriptableObject
{
    [SerializeField] private string fileName = "audio_settings";
    [SerializeField] private List<AudioDataItem> items = new List<AudioDataItem>();

    public List<AudioDataItem> Items => items;

    private string FilePath => Path.Combine(Application.persistentDataPath, fileName + ".json");

    public float GetVolume(string name)
    {
        foreach (var item in items)
        {
            if (item.name == name)
                return item.volume;
        }
        return 1f;
    }

    public void SetVolume(string name, float volume)
    {
        foreach (var item in items)
        {
            if (item.name == name)
            {
                item.volume = volume;
                return;
            }
        }
        items.Add(new AudioDataItem { name = name, volume = volume });
    }

    [ContextMenu("Сохранить в файл")]
    public void SaveToFile()
    {
        string dataString = JsonUtility.ToJson(new AudioDataWrapper { items = items }, true);

        if (!File.Exists(FilePath))
        {
            File.Create(FilePath).Close();
        }
        File.WriteAllText(FilePath, dataString);
    }

    [ContextMenu("Загрузить из файла")]
    public void LoadFromFile()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        string dataString = File.ReadAllText(FilePath);
        var wrapper = JsonUtility.FromJson<AudioDataWrapper>(dataString);
        items = wrapper.items;
    }

    [System.Serializable]
    private class AudioDataWrapper
    {
        public List<AudioDataItem> items;
    }
}

[System.Serializable]
public class AudioDataItem
{
    public string name;
    public float volume;
}