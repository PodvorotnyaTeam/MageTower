using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemSingleton : MonoBehaviour
{
    private static EventSystemSingleton instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // Уже есть один глобальный EventSystem — уничтожаем дубликат
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Не уничтожается при загрузке сцен
    }
}