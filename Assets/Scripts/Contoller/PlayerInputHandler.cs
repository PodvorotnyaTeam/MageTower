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

    // True while the inventory or the journal is open.
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
        UpdateOpenState();
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
            return;
        }

        CloseInventory();
        CloseMagazine();
    }

    private void OnInventoryPressed(InputAction.CallbackContext ctx)
    {
        if (panelInventory.activeSelf) CloseInventory();
        else OpenInventory();
    }

    private void OnMagazinePressed(InputAction.CallbackContext ctx)
    {
        if (panelMagazine.activeSelf) CloseMagazine();
        else OpenMagazine();
    }

    private void OpenInventory()
    {
        panelInventory.SetActive(true);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(false);
        if (inventory != null) inventory.OnInventoryOpen();
        UpdateOpenState();
    }

    private void CloseInventory()
    {
        if (!panelInventory.activeSelf) return;

        panelInventory.SetActive(false);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(true);
        if (inventory != null) inventory.OnInventoryClose();
        UpdateOpenState();
    }

    private void OpenMagazine()
    {
        panelMagazine.SetActive(true);
        UpdateOpenState();
    }

    private void CloseMagazine()
    {
        panelMagazine.SetActive(false);
        UpdateOpenState();
    }

    private void UpdateOpenState()
    {
        fromInventory = panelInventory.activeSelf || panelMagazine.activeSelf;
    }

    // Called by UI buttons.
    public void Magazine()
    {
        if (panelMagazine.activeSelf) CloseMagazine();
        else OpenMagazine();
    }

    public void Inventory()
    {
        if (panelInventory.activeSelf) CloseInventory();
        else OpenInventory();
    }

    private void OpenGameMenu()
    {
        Time.timeScale = 0f;
        GameManager.fromGame = true;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}
