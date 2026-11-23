using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject panelSettings;
    public GameObject buttonContinue;
    public GameObject buttonNewGame;
    public GameObject buttonSettings;
    public GameObject buttonExit;
    public GameObject titleMageTower;

    private void Start()
    {
        panelSettings.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            panelSettings.SetActive(false);
        }
    }

    // Play continues the saved game if there is one, otherwise it starts a new one.
    public void Play()
    {
        // The pause menu stops time. A scene that starts with timeScale 0 is frozen: nothing moves.
        Time.timeScale = 1.0f;
        SaveSystem.PrepareToStart(true);
        SceneManager.LoadScene("MainScene");
    }

    public void NewGame()
    {
        Time.timeScale = 1.0f;
        SaveSystem.PrepareToStart(false);
        SceneManager.LoadScene("MainScene");
    }

    public void Continue()
    {
        Time.timeScale = 1.0f;
        SceneManager.UnloadSceneAsync("MainMenu");
    }

    public void Back()
    {
        SaveSystem.Save();
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Settings()
    {
        titleMageTower.SetActive(!titleMageTower.activeSelf);
        buttonContinue.SetActive(!buttonContinue.activeSelf);
        buttonExit.SetActive(!buttonExit.activeSelf);
        buttonNewGame.SetActive(!buttonNewGame.activeSelf);
        buttonSettings.SetActive(!buttonSettings.activeSelf);
        panelSettings.SetActive(!panelSettings.activeSelf);
    }

    public void Exit()
    {
        Debug.Log("Вы вышли из игры");
        Application.Quit();
    }
}
