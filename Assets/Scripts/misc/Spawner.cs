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

        Instantiate(worldItemPrefab, spawnLocation);
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