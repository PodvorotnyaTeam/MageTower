using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pocket. F puts the held item into the hotbar (or the inventory when the
/// hotbar is full) and, with empty hands, takes the selected hotbar item back
/// into the hand. Number keys 1-9 pick the hotbar slot.
/// </summary>
public class PlayerPocket : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Key pocketKey = Key.F;

    private bool hotbarPrepared;

    private void Update()
    {
        if (interactor == null)
            interactor = FindFirstObjectByType<PlayerInteractor>();

        if (inventory == null)
            inventory = Inventory.Instance;

        if (interactor == null || inventory == null || Keyboard.current == null)
            return;

        // The inventory, the journal or a shop book is open: the keys belong to the UI.
        if (PlayerInputHandler.fromInventory || MageTower.Shop.BookStation.IsAnyBookOpen)
            return;

        PrepareHotbar();
        SelectSlotWithNumberKeys();

        if (!Keyboard.current[pocketKey].wasPressedThisFrame)
            return;

        if (interactor.HeldObject != null)
            Stow();
        else
            TakeOut();
    }

    // The slot prefab has no Outline, so the selected slot was never marked
    // and GetSelectedHotbarItem always returned null. Add it once.
    private void PrepareHotbar()
    {
        if (hotbarPrepared || inventory.gameHotbarGrid == null || inventory.gameHotbarGrid.childCount == 0)
            return;

        foreach (Transform slot in inventory.gameHotbarGrid)
        {
            Outline outline = slot.GetComponent<Outline>();

            if (outline == null)
            {
                outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(4f, -4f);
            }

            outline.enabled = false;
        }

        inventory.SetSelectedHotbarSlot(0);
        hotbarPrepared = true;
    }

    private void SelectSlotWithNumberKeys()
    {
        for (int i = 0; i < inventory.hotbarSize && i < 9; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i);

            if (Keyboard.current[key].wasPressedThisFrame)
                inventory.SetSelectedHotbarSlot(i);
        }
    }

    private void Stow()
    {
        GameObject held = interactor.HeldObject.gameObject;
        int itemId = inventory.FindItemIdByWorldObject(held);

        if (itemId == inventory.emptyItemId)
        {
            Debug.Log($"[Pocket] {held.name} is not in the item database, it cannot be put in the pocket.");
            return;
        }

        // An item with its own state (enchantments, potion rarity) keeps it: the state travels with the slot.
        object payload = PocketState.Capture(held);

        bool stored = payload != null
            ? inventory.AddItemWithPayload(itemId, payload)
            : inventory.AddItemToHotbar(itemId, 1) || inventory.AddItemToInventory(itemId, 1);

        if (!stored)
        {
            Debug.Log("[Pocket] No room in the hotbar or the inventory.");
            return;
        }

        interactor.ReleaseHeld();
        Destroy(held);
        inventory.RefreshGameHotbar();
    }

    private void TakeOut()
    {
        ItemSlot slot = inventory.GetSelectedHotbarItem();

        if (slot == null || slot.itemId == inventory.emptyItemId || slot.count <= 0)
            return;

        GameObject prefab = inventory.GetWorldPrefab(slot.itemId);

        if (prefab == null)
        {
            Debug.Log($"[Pocket] Item {slot.itemId} has no world prefab in the database.");
            return;
        }

        object payload = slot.payload;

        inventory.TakeOneFromSelectedHotbarSlot();

        GameObject item = Instantiate(prefab, interactor.holdPoint.position, interactor.holdPoint.rotation);

        PocketState.Restore(item, payload);

        IGrabbable grabbable = item.GetComponent<IGrabbable>();

        if (grabbable == null)
            grabbable = item.AddComponent<IGrabbable>();

        interactor.Grab(grabbable);
    }
}
