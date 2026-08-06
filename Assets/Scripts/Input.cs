using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInputHandler : MonoBehaviour
{
    public GameObject panelMagazine;
    public GameObject panelInventory;
    public Inventory inventory;
    public GameObject gameHotbarPanel;

    public static bool fromInventory = false;

    private void Start()
    {
        panelInventory.SetActive(false);
        panelMagazine.SetActive(false);

        gameHotbarPanel.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!fromInventory)
            {
                GameManager.fromGame = true;
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                CloseInventory();
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (fromInventory)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (fromInventory)
            {
                CloseInventory();
                panelMagazine.SetActive(false);
            }
            else
            {
                OpenInventory();
                panelMagazine.SetActive(true);
            }
        }
    }

    private void OpenInventory()
    {
        fromInventory = true;
        panelInventory.SetActive(true);

        if (gameHotbarPanel != null)
            gameHotbarPanel.SetActive(false);

        if (inventory != null)
            inventory.OnInventoryOpen();
    }

    private void CloseInventory()
    {
        fromInventory = false;
        panelInventory.SetActive(false);
        panelMagazine.SetActive(false);

        if (gameHotbarPanel != null)
            gameHotbarPanel.SetActive(true);

        if (inventory != null)
            inventory.OnInventoryClose();
    }

    public void Magazine()
    {
        panelMagazine.SetActive(!panelMagazine.activeSelf);
    }
}