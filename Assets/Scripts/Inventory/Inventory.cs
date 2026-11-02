using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        //AddTestItems(); // random test items, off now that real items exist
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

    // ==================== ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ INPUT СКРИПТА ====================

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

    // Установить выбранный слот хотбара (по индексу)
    public void SetSelectedHotbarSlot(int index)
    {
        for (int i = 0; i < gameHotbarUISlots.Count; i++)
        {
            var outline = gameHotbarUISlots[i]?.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = (i == index);
        }
    }

    // ==================== ИНИЦИАЛИЗАЦИЯ ====================

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

    // ==================== UI ОБНОВЛЕНИЕ ====================

    //Временный метод, пока не появились спрайты для айтемов
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

        if (useDebugColors)
        {
            if (slotImage != null)
            {
                slotImage.sprite = null;
                slotImage.color = GetColorById(slot.itemId);
            }

            if (countText != null)
            {
                Debug.Log($"[UpdateSlotUI] slot.itemId={slot.itemId}, slot.count={slot.count}, text={countText.name}");

                countText.supportRichText = true;
                countText.resizeTextForBestFit = false;
                // ID на первой строке, количество — на второй. Оба видны всегда.
                countText.text = $"<size=14>ID:{slot.itemId}</size>\n<size=20>x{slot.count}</size>";
            }
        }
        else
        {
            if (slotImage != null)
            {
                slotImage.color = PocketState.TintOf(slot.payload);
                if (data != null && data.items.Count > slot.itemId)
                    slotImage.sprite = data.items[slot.itemId].img;
            }

            if (countText != null)
                countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }
    //private void UpdateSlotUI(ItemSlot slot)
    //{
    //    if (slot.slotObject == null) return;

    //    Image slotImage = slot.slotObject.GetComponent<Image>();
    //    Text countText = slot.slotObject.GetComponentInChildren<Text>();

    //    if (slot.itemId == emptyItemId || slot.count <= 0)
    //    {
    //        // Пустой слот - показываем empty иконку
    //        if (data != null && data.items.Count > emptyItemId)
    //            slotImage.sprite = data.items[emptyItemId].img;
    //        else
    //            slotImage.sprite = null;

    //        if (countText != null)
    //            countText.text = "";
    //    }
    //    else
    //    {
    //        // Слот с предметом
    //        if (data != null && data.items.Count > slot.itemId)
    //            slotImage.sprite = data.items[slot.itemId].img;

    //        if (countText != null)
    //            countText.text = slot.count > 1 ? slot.count.ToString() : "";
    //    }
    //}

    //Временный метод, пока не появились спрайты для айтемов
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

            if (useDebugColors)
            {
                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.color = GetColorById(slotData.itemId);
                }
                if (countText != null)
                {
                    countText.supportRichText = true;
                    string idPart = $"<size=30>ID{slotData.itemId}</size>";
                    string countPart = slotData.count > 1 ? $"\n{slotData.count}" : "";
                    countText.text = idPart + countPart;
                }
            }
            else
            {
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
    }
    //private void UpdateGameHotbarUI()
    //{
    //    for (int i = 0; i < hotbarSize && i < gameHotbarUISlots.Count && i < hotbarSlots.Count; i++)
    //    {
    //        var slotData = hotbarSlots[i];
    //        var uiObject = gameHotbarUISlots[i];

    //        if (uiObject == null) continue;

    //        Image slotImage = uiObject.GetComponent<Image>();
    //        Text countText = uiObject.GetComponentInChildren<Text>();

    //        if (slotData.itemId == emptyItemId || slotData.count <= 0)
    //        {
    //            if (data != null && data.items.Count > emptyItemId)
    //                slotImage.sprite = data.items[emptyItemId].img;
    //            if (countText != null) countText.text = "";
    //        }
    //        else
    //        {
    //            if (data != null && data.items.Count > slotData.itemId)
    //                slotImage.sprite = data.items[slotData.itemId].img;
    //            if (countText != null) countText.text = slotData.count > 1 ? slotData.count.ToString() : "";
    //        }
    //    }
    //}

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

    // ==================== ДОБАВЛЕНИЕ ПРЕДМЕТОВ ====================

    private void AddTestItems()
    {
        if (data == null) return;

        // Добавляем тестовые предметы в инвентарь
        for (int i = 0; i < inventorySize / 3; i++)
        {
            if (data.items.Count > 1)
            {
                int randomItem = Random.Range(1, data.items.Count);
                int randomCount = Random.Range(1, 65);
                AddItemToInventory(randomItem, randomCount);
            }
        }

        // Добавляем тестовые предметы в хотбар
        for (int i = 0; i < hotbarSize / 2; i++)
        {
            if (data.items.Count > 1)
            {
                int randomItem = Random.Range(1, data.items.Count);
                int randomCount = Random.Range(1, 65);
                AddItemToHotbar(randomItem, randomCount);
            }
        }
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

    // ---- pocket helpers (PlayerPocket)

    public GameObject GetWorldPrefab(int itemId)
    {
        if (data == null || itemId <= emptyItemId || itemId >= data.items.Count)
            return null;

        return data.items[itemId].worldPrefab;
    }

    // Finds the item id of a world object by the name of the prefab it was made from.
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

    // Takes one item out of the selected hotbar slot and returns its id (emptyItemId if there is none).
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

    // Puts one item that carries its own state into an empty hotbar slot, or an empty
    // inventory slot. Such items never stack.
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

    // ---- save / load (SaveSystem)

    public System.Collections.Generic.IReadOnlyList<ItemSlot> HotbarSlots => hotbarSlots;
    public System.Collections.Generic.IReadOnlyList<ItemSlot> InventorySlots => inventorySlots;

    public void RefreshAll()
    {
        UpdateAllUI();
    }

    public bool AddItemToInventory(int itemId, int count)
    {
        // Сначала ищем существующие стаки
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventorySlots[i].itemId == itemId && inventorySlots[i].count < 64 && inventorySlots[i].payload == null)
            {
                int remaining = AddToSlot(i, SlotType.Inventory, count);
                if (remaining == 0) return true;
                count = remaining;
            }
        }

        // Ищем пустые слоты
        for (int i = 0; i < inventorySize; i++)
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
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i].itemId == itemId && hotbarSlots[i].count < 64 && hotbarSlots[i].payload == null)
            {
                int remaining = AddToSlot(i, SlotType.Hotbar, count);
                if (remaining == 0) return true;
                count = remaining;
            }
        }

        for (int i = 0; i < hotbarSize; i++)
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

    // ==================== ЛОГИКА ПЕРЕМЕЩЕНИЯ ====================

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
                if (useDebugColors)
                {
                    dragImage.sprite = null;
                    dragImage.color = GetColorById(slot.itemId);
                }
                else if (data != null && data.items.Count > slot.itemId)
                {
                    dragImage.color = Color.white;
                    dragImage.sprite = data.items[slot.itemId].img;
                }
                //if (data != null && data.items.Count > slot.itemId)
                //    draggingItem.GetComponent<Image>().sprite = data.items[slot.itemId].img;

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

    // An item that is still on the cursor goes back into the slot it was picked from.
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

    // ==================== HELPER МЕТОДЫ ====================

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

    // State of a single item that must survive the trip through the inventory
    // (an enchanted sword). A slot with a payload never stacks.
    public object payload;
    public GameObject slotObject;
    public int slotIndex;
    public SlotType type;
}