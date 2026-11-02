using NUnit.Framework.Internal.Execution;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("References")]
    public DatabaseForInventory data;
    public GameObject slotPrefab;

    [Header("UI Grids")]
    public Transform inventoryGrid;
    public Transform hotbarGrid;
    public Transform gameHotbarGrid;

    [Header("Settings")]
    public int inventorySize = 27;
    public int hotbarSize = 9;
    public int emptyItemId = 0;

    [Header("Drag & Drop")]
    public RectTransform draggingItem;
    public Vector3 dragOffset;
    public Canvas mainCanvas;

    // Данные (единственное хранилище)
    private List<ItemSlot> inventorySlots = new List<ItemSlot>();
    private List<ItemSlot> hotbarSlots = new List<ItemSlot>();

    // UI элементы (два разных представления для хотбара)
    private List<GameObject> hotbarUISlots = new List<GameObject>();
    private List<GameObject> gameHotbarUISlots = new List<GameObject>();

    // Drag & Drop
    private int selectedSlotId = -1;
    private ItemSlot selectedItem;
    private bool isDraggingFromHotbar = false;

    public static Inventory Instance { get; private set; }

    [HideInInspector] public int currentSelectedHotbarIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeInventoryGrid();
        InitializeHotbarUI();
        UpdateAllUI();
    }

    private void Update()
    {
        if (selectedSlotId != -1)
            DragItem();
    }

    [Header("Debug")]
    [Tooltip("Показывать ID цветом и текстом вместо спрайтов")]
    public bool useDebugColors = true;

    private Color GetColorById(int id)
    {
        // Золотое сечение даёт равномерно распределённые оттенки
        float hue = (id * 0.6180339887f) % 1f;
        return Color.HSVToRGB(hue, 0.65f, 0.85f);
    }

    // Вызывается, когда инвентарь открывается
    public void OnInventoryOpen()
    {
        UpdateAllUI();
    }

    // Вызывается, когда инвентарь закрывается
    public void OnInventoryClose()
    {
        ReturnDraggedItem();
        ClearDragSelection();
    }

    // Обновить игровой хотбар (вызывать при добавлении/удалении предметов)
    public void RefreshGameHotbar()
    {
        UpdateGameHotbarUI();
    }

    // Получить предмет из выбранного слота хотбара (для использования)
    public ItemSlot GetSelectedHotbarItem()
    {
        // Возвращает выбранный предмет (слот 0 по умолчанию или по индексу)
        // Твоя логика выбора слота может быть отдельно
        for (int i = 0; i < hotbarSize; i++)
        {
            var outline = gameHotbarUISlots[i]?.GetComponent<Outline>();
            if (outline != null && outline.enabled)
                return hotbarSlots[i];
        }
        return null;
    }

    private void InitializeInventoryGrid()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slot = Instantiate(slotPrefab, inventoryGrid);
            slot.name = $"InventorySlot_{i}";

            ItemSlot itemSlot = new ItemSlot
            {
                itemId = emptyItemId,
                count = 0,
                slotObject = slot,
                slotIndex = i,
                type = SlotType.Inventory
            };

            int index = i;
            Button button = slot.GetComponent<Button>();
            button.onClick.AddListener(() => SelectSlot(index, SlotType.Inventory));

            inventorySlots.Add(itemSlot);
        }
    }

    private void InitializeHotbarUI()
    {
        // 1. ХОТБАР ВНУТРИ МЕНЮ
        for (int i = 0; i < hotbarSize; i++)
        {
            GameObject slot = Instantiate(slotPrefab, hotbarGrid);
            slot.name = $"MenuHotbarSlot_{i}";

            ItemSlot itemSlot = new ItemSlot
            {
                itemId = emptyItemId,
                count = 0,
                slotObject = slot,
                slotIndex = i,
                type = SlotType.Hotbar
            };

            int index = i;
            Button button = slot.GetComponent<Button>();
            button.onClick.AddListener(() => SelectSlot(index, SlotType.Hotbar));

            hotbarSlots.Add(itemSlot);
            hotbarUISlots.Add(slot);
        }

        // 2. ХОТБАР ВНЕ МЕНЮ (только UI, данные из hotbarSlots)
        if (gameHotbarGrid != null)
        {
            for (int i = 0; i < hotbarSize; i++)
            {
                GameObject gameSlot = Instantiate(slotPrefab, gameHotbarGrid);
                gameSlot.name = $"GameHotbarSlot_{i}";

                int index = i;
                Button button = gameSlot.GetComponent<Button>();
                button.onClick.AddListener(() => SelectGameHotbarSlot(index));

                gameHotbarUISlots.Add(gameSlot);
            }
        }
    }

    private void SelectGameHotbarSlot(int index)
    {
        // Логика клика по игровому хотбару (вне меню)
        Debug.Log($"Клик по игровому хотбару, слот {index}");
        SetSelectedHotbarSlot(index);

        // Здесь можно добавить использование предмета по двойному клику и т.д.
    }

    private void UpdateSlotUI(ItemSlot slot)
    {
        if (slot.slotObject == null) return;

        Image slotImage = slot.slotObject.GetComponent<Image>();
        Text countText = slot.slotObject.GetComponentInChildren<Text>();

        bool isEmpty = (slot.itemId == emptyItemId || slot.count <= 0);

        if (isEmpty)
        {
            if (slotImage != null)
            {
                slotImage.color = Color.white;
                slotImage.sprite = (data != null && data.items.Count > emptyItemId)
                    ? data.items[emptyItemId].img
                    : null;
            }
            if (countText != null) countText.text = "";
            return;
        }

        if (slotImage != null)
        {
            slotImage.color = PocketState.TintOf(slot.payload);
            if (data != null && data.items.Count > slot.itemId)
                slotImage.sprite = data.items[slot.itemId].img;
        }

        if (countText != null)
            countText.text = slot.count > 1 ? slot.count.ToString() : "";
    }

    //private void UpdateSlotUI(ItemSlot slot)
    //{
    //    if (slot.slotObject == null) return;

    //    Transform itemTransform = slot.slotObject.transform.Find("Item");
    //    Image slotImage = itemTransform != null ? itemTransform.GetComponent<Image>() : null;
    //    Text countText = itemTransform != null ? itemTransform.GetComponentInChildren<Text>() : null;

    //    if (slot.itemId == emptyItemId || slot.count <= 0)
    //    {
    //        if (slotImage != null)
    //            slotImage.sprite = (data != null && data.items.Count > emptyItemId) ? data.items[emptyItemId].img : null;

    //        if (countText != null) countText.text = "";
    //        return;
    //    }

    //    if (slotImage != null && data != null && data.items.Count > slot.itemId)
    //        slotImage.sprite = data.items[slot.itemId].img;

    //    if (countText != null)
    //        countText.text = slot.count > 1 ? slot.count.ToString() : "";
    //}

    private void UpdateGameHotbarUI()
    {
        for (int i = 0; i < hotbarSize && i < gameHotbarUISlots.Count && i < hotbarSlots.Count; i++)
        {
            var slotData = hotbarSlots[i];
            var uiObject = gameHotbarUISlots[i];
            if (uiObject == null) continue;

            Image slotImage = uiObject.GetComponent<Image>();
            Text countText = uiObject.GetComponentInChildren<Text>();

            bool isEmpty = (slotData.itemId == emptyItemId || slotData.count <= 0);

            if (isEmpty)
            {
                if (slotImage != null)
                {
                    slotImage.color = Color.white;
                    if (data != null && data.items.Count > emptyItemId)
                        slotImage.sprite = data.items[emptyItemId].img;
                }
                if (countText != null) countText.text = "";
                continue;
            }

            if (slotImage != null)
            {
                slotImage.color = PocketState.TintOf(slotData.payload);
                if (data != null && data.items.Count > slotData.itemId)
                    slotImage.sprite = data.items[slotData.itemId].img;
            }
            if (countText != null)
                countText.text = slotData.count > 1 ? slotData.count.ToString() : "";
        }
    }

    //private void UpdateGameHotbarUI()
    //{
    //    for (int i = 0; i < hotbarSize && i < gameHotbarUISlots.Count && i < hotbarSlots.Count; i++)
    //    {
    //        var slotData = hotbarSlots[i];
    //        var uiObject = gameHotbarUISlots[i];
    //        if (uiObject == null) continue;

    //        Transform itemTransform = uiObject.transform.Find("Item");
    //        Image slotImage = itemTransform != null ? itemTransform.GetComponent<Image>() : null;
    //        Text countText = itemTransform != null ? itemTransform.GetComponentInChildren<Text>() : null;

    //        if (slotData.itemId == emptyItemId || slotData.count <= 0)
    //        {
    //            if (slotImage != null)
    //                slotImage.sprite = (data != null && data.items.Count > emptyItemId) ? data.items[emptyItemId].img : null;

    //            if (countText != null) countText.text = "";
    //            continue;
    //        }

    //        if (slotImage != null && data != null && data.items.Count > slotData.itemId)
    //            slotImage.sprite = data.items[slotData.itemId].img;

    //        if (countText != null)
    //            countText.text = slotData.count > 1 ? slotData.count.ToString() : "";
    //    }
    //}

    public void CycleHotbarSlot(int direction)
    {
        int newIndex = currentSelectedHotbarIndex + direction;
        if (newIndex < 0) newIndex = hotbarSize - 1;
        if (newIndex >= hotbarSize) newIndex = 0;
        SetSelectedHotbarSlot(newIndex);
    }

    public void SetSelectedHotbarSlot(int index)
    {
        currentSelectedHotbarIndex = Mathf.Clamp(index, 0, hotbarSize - 1);
        for (int i = 0; i < gameHotbarUISlots.Count; i++)
        {
            var outline = gameHotbarUISlots[i]?.GetComponent<Outline>();
            if (outline != null) outline.enabled = (i == currentSelectedHotbarIndex);
        }
    }

    private void UpdateAllUI()
    {
        foreach (var slot in inventorySlots)
            UpdateSlotUI(slot);

        foreach (var slot in hotbarSlots)
            UpdateSlotUI(slot);

        UpdateGameHotbarUI();
    }

    private void ClearSlot(ItemSlot slot)
    {
        slot.itemId = emptyItemId;
        slot.count = 0;
        slot.payload = null;
    }

    /// <summary>
    /// Пытается добавить предмет в инвентарь. Если места нет — пробует хотбар.
    /// Возвращает true, если предмет удалось положить хотя бы частично.
    /// </summary>
    public bool TryAddItem(int itemId, int count = 1)
    {
        if (itemId == emptyItemId || count <= 0)
            return false;

        // 1. Пробуем инвентарь
        if (AddItemToInventory(itemId, count))
            return true;

        // 2. Если инвентарь полон — пробуем хотбар
        if (AddItemToHotbar(itemId, count))
        {
            UpdateGameHotbarUI();
            return true;
        }

        return false;
    }

    public GameObject GetWorldPrefab(int itemId)
    {
        if (data == null || itemId <= emptyItemId || itemId >= data.items.Count)
            return null;

        return data.items[itemId].worldPrefab;
    }

    public int FindItemIdByWorldObject(GameObject worldObject)
    {
        if (data == null || worldObject == null)
            return emptyItemId;

        string prefabName = System.Text.RegularExpressions.Regex
            .Replace(worldObject.name, @"(\s*\(Clone\)|\s\(\d+\))+$", "")
            .Trim();

        for (int i = emptyItemId + 1; i < data.items.Count; i++)
        {
            if (data.items[i].worldPrefab != null && data.items[i].worldPrefab.name == prefabName)
                return i;
        }

        return emptyItemId;
    }

    public int TakeOneFromSelectedHotbarSlot()
    {
        ItemSlot slot = GetSelectedHotbarItem();

        if (slot == null || slot.itemId == emptyItemId || slot.count <= 0)
            return emptyItemId;

        int itemId = slot.itemId;
        slot.count--;

        if (slot.count <= 0)
            ClearSlot(slot);

        UpdateSlotUI(slot);
        UpdateGameHotbarUI();
        return itemId;
    }

    public bool AddItemWithPayload(int itemId, object payload)
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == emptyItemId)
            {
                hotbarSlots[i].itemId = itemId;
                hotbarSlots[i].count = 1;
                hotbarSlots[i].payload = payload;
                UpdateSlotUI(hotbarSlots[i]);
                UpdateGameHotbarUI();
                return true;
            }
        }

        for (int i = 0; i < inventorySize; i++)
        {
            if (inventorySlots[i].itemId == emptyItemId)
            {
                inventorySlots[i].itemId = itemId;
                inventorySlots[i].count = 1;
                inventorySlots[i].payload = payload;
                UpdateSlotUI(inventorySlots[i]);
                return true;
            }
        }

        return false;
    }

    public System.Collections.Generic.IReadOnlyList<ItemSlot> HotbarSlots => hotbarSlots;
    public System.Collections.Generic.IReadOnlyList<ItemSlot> InventorySlots => inventorySlots;

    public void RefreshAll()
    {
        UpdateAllUI();
    }

    public bool AddItemToInventory(int itemId, int count)
    {
        if (data == null || itemId <= emptyItemId || itemId >= data.items.Count)
        {
            Debug.LogWarning($"[Inventory] Некорректный ID: {itemId}");
            return false;
        }

        // Ищем стаки
        for (int i = 0; i < inventorySlots.Count; i++) // <-- ИСПОЛЬЗУЕМ Count
        {
            if (inventorySlots[i].itemId == itemId && inventorySlots[i].count < 64 && inventorySlots[i].payload == null)
            {
                int remaining = AddToSlot(i, SlotType.Inventory, count);
                if (remaining == 0) return true;
                count = remaining;
            }
        }

        // Ищем пустые слоты
        for (int i = 0; i < inventorySlots.Count; i++) // <-- ИСПОЛЬЗУЕМ Count
        {
            if (inventorySlots[i].itemId == emptyItemId)
            {
                inventorySlots[i].itemId = itemId;
                inventorySlots[i].count = Mathf.Min(count, 64);
                UpdateSlotUI(inventorySlots[i]);
                return true;
            }
        }

        return false;
    }

    public bool AddItemToHotbar(int itemId, int count)
    {
        if (data == null || itemId <= emptyItemId || itemId >= data.items.Count)
        {
            Debug.LogWarning($"[Inventory] Некорректный ID: {itemId}");
            return false;
        }

        for (int i = 0; i < hotbarSlots.Count; i++) // <-- ИСПОЛЬЗУЕМ Count
        {
            if (hotbarSlots[i].itemId == itemId && hotbarSlots[i].count < 64 && hotbarSlots[i].payload == null)
            {
                int remaining = AddToSlot(i, SlotType.Hotbar, count);
                if (remaining == 0) return true;
                count = remaining;
            }
        }

        for (int i = 0; i < hotbarSlots.Count; i++) // <-- ИСПОЛЬЗУЕМ Count
        {
            if (hotbarSlots[i].itemId == emptyItemId)
            {
                hotbarSlots[i].itemId = itemId;
                hotbarSlots[i].count = Mathf.Min(count, 64);
                UpdateSlotUI(hotbarSlots[i]);
                UpdateGameHotbarUI();
                return true;
            }
        }

        return false;
    }

    private int AddToSlot(int slotIndex, SlotType type, int count)
    {
        var slot = GetSlot(type, slotIndex);
        int spaceLeft = 64 - slot.count;
        int toAdd = Mathf.Min(count, spaceLeft);

        slot.count += toAdd;
        UpdateSlotUI(slot);

        if (type == SlotType.Hotbar)
            UpdateGameHotbarUI();

        return count - toAdd;
    }

    private void SelectSlot(int slotIndex, SlotType type)
    {
        Debug.Log($"[SelectSlot] index={slotIndex}, type={type}, selectedSlotId={selectedSlotId}");

        var slot = GetSlot(type, slotIndex);

        if (selectedSlotId == -1)
        {
            // ВЗЯТЬ предмет
            if (slot.itemId != emptyItemId && slot.count > 0)
            {
                selectedSlotId = slotIndex;
                selectedItem = CopySlot(slot);
                isDraggingFromHotbar = (type == SlotType.Hotbar);

                draggingItem.gameObject.SetActive(true);

                //Временно, пока не появились спрайты
                var dragImage = draggingItem.GetComponent<Image>();
                if (data != null && data.items.Count > slot.itemId)
                    dragImage.sprite = data.items[slot.itemId].img;

                ClearSlot(slot);
                UpdateSlotUI(slot);

                if (type == SlotType.Hotbar)
                    UpdateGameHotbarUI();
            }
        }
        else
        {
            // ПОЛОЖИТЬ предмет
            if (slot.itemId == emptyItemId)
            {
                MoveToEmptySlot(slot, type);
            }
            else if (slot.itemId == selectedItem.itemId && slot.payload == null && selectedItem.payload == null)
            {
                MergeIntoSlot(slot, type);
            }
            else
            {
                SwapWithSlot(slot, type);
            }

            ClearDragSelection();
        }
    }

    private void MoveToEmptySlot(ItemSlot targetSlot, SlotType targetType)
    {
        targetSlot.itemId = selectedItem.itemId;
        targetSlot.count = selectedItem.count;
        targetSlot.payload = selectedItem.payload;
        UpdateSlotUI(targetSlot);

        if (targetType == SlotType.Hotbar || isDraggingFromHotbar)
            UpdateGameHotbarUI();
    }

    private void MergeIntoSlot(ItemSlot targetSlot, SlotType targetType)
    {
        int total = targetSlot.count + selectedItem.count;

        if (total <= 64)
        {
            targetSlot.count = total;
            UpdateSlotUI(targetSlot);
        }
        else
        {
            targetSlot.count = 64;
            selectedItem.count = total - 64;
            UpdateSlotUI(targetSlot);

            var sourceSlot = GetSlot(
                isDraggingFromHotbar ? SlotType.Hotbar : SlotType.Inventory,
                selectedSlotId
            );
            sourceSlot.itemId = selectedItem.itemId;
            sourceSlot.count = selectedItem.count;
            UpdateSlotUI(sourceSlot);

            selectedSlotId = -1;
            selectedItem = null;
            draggingItem.gameObject.SetActive(false);
            return;
        }

        if (targetType == SlotType.Hotbar || isDraggingFromHotbar)
            UpdateGameHotbarUI();
    }

    private void SwapWithSlot(ItemSlot targetSlot, SlotType targetType)
    {
        ItemSlot tempSlot = CopySlot(targetSlot);

        targetSlot.itemId = selectedItem.itemId;
        targetSlot.count = selectedItem.count;
        targetSlot.payload = selectedItem.payload;
        UpdateSlotUI(targetSlot);

        var sourceSlot = GetSlot(
            isDraggingFromHotbar ? SlotType.Hotbar : SlotType.Inventory,
            selectedSlotId
        );
        sourceSlot.itemId = tempSlot.itemId;
        sourceSlot.count = tempSlot.count;
        sourceSlot.payload = tempSlot.payload;
        UpdateSlotUI(sourceSlot);

        if (targetType == SlotType.Hotbar || isDraggingFromHotbar)
            UpdateGameHotbarUI();
    }

    private void ReturnDraggedItem()
    {
        if (selectedSlotId == -1 || selectedItem == null)
            return;

        var source = GetSlot(isDraggingFromHotbar ? SlotType.Hotbar : SlotType.Inventory, selectedSlotId);

        if (source.itemId != emptyItemId)
            return;

        source.itemId = selectedItem.itemId;
        source.count = selectedItem.count;
        source.payload = selectedItem.payload;
        UpdateSlotUI(source);
        UpdateGameHotbarUI();
    }

    private void ClearDragSelection()
    {
        selectedSlotId = -1;
        selectedItem = null;
        draggingItem.gameObject.SetActive(false);
    }

    private void DragItem()
    {
        Vector3 mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
        draggingItem.position = mousePos + dragOffset;
    }

    private ItemSlot GetSlot(SlotType type, int index)
    {
        return type == SlotType.Inventory ? inventorySlots[index] : hotbarSlots[index];
    }

    private ItemSlot CopySlot(ItemSlot original)
    {
        return new ItemSlot
        {
            itemId = original.itemId,
            count = original.count,
            payload = original.payload,
            slotObject = original.slotObject,
            slotIndex = original.slotIndex,
            type = original.type
        };
    }

    public void DropFromSelectedHotbar(Vector3 spawnPos, Vector3 forward)
    {
        var slot = hotbarSlots[currentSelectedHotbarIndex];
        if (slot.itemId == emptyItemId || slot.count <= 0) return;

        var itemData = data.items[slot.itemId];
        if (itemData.worldPrefab == null) return;

        // Спавним префаб перед камерой
        Vector3 finalSpawnPos = spawnPos + forward * 1.2f;
        GameObject spawned = Instantiate(itemData.worldPrefab, finalSpawnPos, Quaternion.identity);

        // Настраиваем WorldItem
        var worldItem = spawned.GetComponent<WorldItem>();
        if (worldItem == null) worldItem = spawned.AddComponent<WorldItem>();
        worldItem.itemId = slot.itemId;
        worldItem.count = 1;

        // Убираем 1 предмет из слота
        slot.count--;
        if (slot.count <= 0) ClearSlot(slot);

        UpdateSlotUI(slot);
        UpdateGameHotbarUI();
    }
}

public enum SlotType
{
    Inventory,
    Hotbar
}

[System.Serializable]
public class ItemSlot
{
    public int itemId;
    public int count;

    public object payload;
    public GameObject slotObject;
    public int slotIndex;
    public SlotType type;
}

