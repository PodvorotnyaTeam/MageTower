using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject panelMagazine;
    [SerializeField] private GameObject panelInventory;
    [SerializeField] private GameObject gameHotbarPanel;
    [SerializeField] private Inventory inventory;

    public static bool fromInventory = false;

    private Control control;

    private void Awake()
    {
        control = new Control();

        control.Player.Pause.performed += OnPausePressed;
        control.Player.Inventory.performed += OnInventoryPressed;
        control.Player.Magazine.performed += OnMagazinePressed;
    }

    private void Start()
    {
        panelInventory.SetActive(false);
        panelMagazine.SetActive(false);
        gameHotbarPanel.SetActive(true);
    }

    private void OnEnable() => control.Player.Enable();
    private void OnDisable() => control.Player.Disable();

    private void OnDestroy()
    {
        control.Player.Pause.performed -= OnPausePressed;
        control.Player.Inventory.performed -= OnInventoryPressed;
        control.Player.Magazine.performed -= OnMagazinePressed;
        control.Dispose();
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (!fromInventory)
        {
            OpenGameMenu();
        }
        else
        {
            CloseInventory();
        }
    }

    private void OnInventoryPressed(InputAction.CallbackContext ctx)
    {
        if (fromInventory) CloseInventory();
        else OpenInventory();
    }

    private void OnMagazinePressed(InputAction.CallbackContext ctx)
    {
        if (fromInventory) Magazine();
        else CloseInventory();
    }

    private void OpenInventory()
    {
        fromInventory = true;
        panelInventory.SetActive(true);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(false);
        if (inventory != null) inventory.OnInventoryOpen();
    }

    private void CloseInventory()
    {
        fromInventory = false;
        panelInventory.SetActive(false);
        panelMagazine.SetActive(false);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(true);
        if (inventory != null) inventory.OnInventoryClose();
    }

    public void Magazine()
    {
        panelMagazine.SetActive(!panelMagazine.activeSelf);
    }

    public void Inventory()
    {
        panelInventory.SetActive(!panelInventory.activeSelf);
    }

    private void OpenGameMenu()
    {
        Time.timeScale = 0f;
        GameManager.fromGame = true;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}