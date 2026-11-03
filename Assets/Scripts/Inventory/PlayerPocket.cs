using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPocket : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Transform playerCamera; // Камера для спавна выброшенного предмета

    private void Update()
    {
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (inventory == null) inventory = Inventory.Instance;
        if (playerCamera == null) playerCamera = Camera.main.transform;

        if (interactor == null || inventory == null || Keyboard.current == null) return;
        if (PlayerInputHandler.fromInventory) return; // Если инвентарь открыт - клавиши для UI

        // Выбор хотбара (Цифры 1-9)
        for (int i = 0; i < 9; i++)
        {
            if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                inventory.SetSelectedHotbarSlot(i);
        }

        // Колесико мыши
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0.1f) inventory.CycleHotbarSlot(-1);
        else if (scroll < -0.1f) inventory.CycleHotbarSlot(1);

        // F - Взять/отпустить предмет в мире (Таскание)
        if (Keyboard.current[Key.F].wasPressedThisFrame)
        {
            if (interactor.HeldObject != null)
            {
                interactor.ReleaseHeld(); // Отпускаем
            }
            else
            {
                // Пытаемся взять то, на что смотрим (только IGrabbable)
                var target = interactor.FindTargetForGrab();
                if (target != null) interactor.Grab(target);
            }
        }

        // Q - Выбросить предмет из активного слота хотбара (один из стака)
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            inventory.DropFromSelectedHotbar(playerCamera.position, playerCamera.forward);
        }
    }
}