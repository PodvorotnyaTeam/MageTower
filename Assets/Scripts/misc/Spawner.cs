using UnityEngine;

public class Spawner : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [Tooltip("ID предмета в DatabaseForInventory")]
    [SerializeField] private int itemId = 1;
    [SerializeField] private int count = 1;

    [Header("Fallback (если инвентарь полон)")]
    [Tooltip("Если true — при полном инвентаре предмет выпадет в сцену. Иначе — не выдастся.")]
    [SerializeField] private bool dropIfInventoryFull = false;
    [SerializeField] private GameObject worldItemPrefab;
    [SerializeField] private Transform spawnLocation;

    [Tooltip("true = spawn worldItemPrefab in the world (ingredient for the cauldron). false = put the item into the inventory.")]
    [SerializeField] private bool spawnInWorld = true;

    private void SpawnInWorld()
    {
        if (worldItemPrefab == null || spawnLocation == null)
        {
            Debug.LogWarning($"[Spawner] {name}: worldItemPrefab or spawnLocation is not assigned.");
            return;
        }

        GameObject item = Instantiate(worldItemPrefab, spawnLocation);
        LiftUntilFree(item);
    }

    // Items spawned one after another at the same point used to overlap and were pushed out of each
    // other so hard that some fell through the floor. Lift the new one until it is clear of loose items.
    private static void LiftUntilFree(GameObject item)
    {
        Collider own = item.GetComponentInChildren<Collider>();

        if (own == null)
            return;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Bounds bounds = own.bounds;
            bool blocked = false;

            foreach (Collider other in Physics.OverlapBox(bounds.center, bounds.extents * 0.95f, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // only other loose items count, not the floor, the box or kinematic display samples
                if (other.transform.IsChildOf(item.transform) || other.attachedRigidbody == null || other.attachedRigidbody.isKinematic)
                    continue;

                blocked = true;
                break;
            }

            if (!blocked)
                return;

            item.transform.position += Vector3.up * (bounds.size.y + 0.02f);
            Physics.SyncTransforms();
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (spawnInWorld)
        {
            SpawnInWorld();
            return;
        }

        Debug.Log($"[4] Spawner.Interact вызван, itemId={itemId}, Instance={(Inventory.Instance != null)}");

        // 1. Пытаемся найти инвентарь
        Inventory inventory = Inventory.Instance;

        // Если синглтон не задан — попробуем найти через игрока/сцену
        if (inventory == null && interactor != null)
            inventory = interactor.GetComponentInParent<Inventory>();

        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        if (inventory == null)
        {
            Debug.LogError("[Spawner] Не найден Inventory в сцене!");
            return;
        }

        // 2. Пытаемся добавить предмет
        bool added = inventory.TryAddItem(itemId, count);

        if (added)
        {
            Debug.Log($"[Spawner] Предмет id={itemId} x{count} добавлен в инвентарь.");
            return;
        }

        // 3. Инвентарь полон
        Debug.LogWarning($"[Spawner] Инвентарь полон! Не удалось добавить id={itemId} x{count}.");

        if (dropIfInventoryFull && worldItemPrefab != null && spawnLocation != null)
        {
            Instantiate(worldItemPrefab, spawnLocation.position, spawnLocation.rotation);
        }
    }
}