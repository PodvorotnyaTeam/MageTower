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
            // switch it off at once: Destroy only takes effect at the end of the frame, and until then
            // Unity would complain that there are two event systems
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Не уничтожается при загрузке сцен
    }
}