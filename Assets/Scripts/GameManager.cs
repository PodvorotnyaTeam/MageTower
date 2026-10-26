using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject panelSettings;
    public GameObject panelGameMenu;

    public static bool fromGame = false;

    private void Start()
    {
        // ���������, ����� �� ���������� GameMenu
        if (fromGame)
        {
            panelSettings.SetActive(false);
            panelGameMenu.SetActive(true);
            fromGame = false; // ���������� ����
        }
        else
        {
            panelSettings.SetActive(false);
            panelGameMenu.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
        fromGame = true; // ������������� ���� ����� ��������� ����
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
        Debug.Log("�� ����� �� ����");
        Application.Quit();
    }
}
