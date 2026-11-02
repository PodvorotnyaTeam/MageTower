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

    // While a shop book is open its own UI handles the keys (this check came with the old Input.cs).
    private static bool ShopBookOpen => MageTower.Shop.BookStation.IsAnyBookOpen;

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (ShopBookOpen)
            return;

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
        if (ShopBookOpen)
            return;

        if (panelInventory.activeSelf) CloseInventory();
        else OpenInventory();
    }

    private void OnMagazinePressed(InputAction.CallbackContext ctx)
    {
        if (ShopBookOpen)
            return;

        if (panelMagazine.activeSelf) CloseMagazine();
        else OpenMagazine();
    }

    private void OpenInventory()
    {
        panelInventory.SetActive(true);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(false);
        CurrentInventory?.OnInventoryOpen();
        UpdateOpenState();
    }

    private void CloseInventory()
    {
        if (!panelInventory.activeSelf) return;

        panelInventory.SetActive(false);
        if (gameHotbarPanel != null) gameHotbarPanel.SetActive(true);
        CurrentInventory?.OnInventoryClose();
        UpdateOpenState();
    }

    // The field is not assigned in the scene, so fall back to the scene's inventory.
    // (global:: because the method Inventory() below hides the class name inside this class)
    private Inventory CurrentInventory => inventory != null ? inventory : global::Inventory.Instance;

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
        SaveSystem.Save();
        Time.timeScale = 0f;
        GameManager.fromGame = true;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}
