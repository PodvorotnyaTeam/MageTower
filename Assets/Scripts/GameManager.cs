using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject panelSettings;
    public GameObject panelGameMenu;

    public static bool fromGame = false;

    private void Start()
    {
        // Проверяем, нужно ли показывать GameMenu
        if (fromGame)
        {
            panelSettings.SetActive(false);
            panelGameMenu.SetActive(true);
            fromGame = false; // Сбрасываем флаг
        }
        else
        {
            panelSettings.SetActive(false);
            panelGameMenu.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            panelSettings.SetActive(false);
        }
    }

    public void Play()
    {
        SceneManager.LoadScene("Game");
    }
    public void BackToMenuFromGame()
    {
        fromGame = true; // Устанавливаем флаг перед загрузкой меню
        SceneManager.LoadScene("MainMenu");
    }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Settings()
    {
        panelSettings.SetActive(!panelSettings.activeSelf);
    }

    public void MainMenu()
    {
        panelGameMenu.SetActive(!panelGameMenu.activeSelf);
    }

    public void Exit()
    {
        Debug.Log("Вы вышли из игры");
        Application.Quit();
    }
}
