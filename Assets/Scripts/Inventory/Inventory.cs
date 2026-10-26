using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    [Header("References")]
    public Database data;
    public GameObject slotPrefab;

    [Header("UI Grids")]
    public Transform inventoryGrid;      // Grid ��� ��������� ���������
    public Transform hotbarGrid;         // Grid ��� ������� ������ ����
    public Transform gameHotbarGrid;     // Grid ��� ������� ��� ����

    [Header("Settings")]
    public int inventorySize = 27;
    public int hotbarSize = 9;
    public int emptyItemId = 0;          // ID ������� �������� � Database

    [Header("Drag & Drop")]
    public RectTransform draggingItem;
    public Vector3 dragOffset;
    public Canvas mainCanvas;
    public EventSystem eventSystem;

    // ������ (������������ ���������)
    private List<ItemSlot> inventorySlots = new List<ItemSlot>();
    private List<ItemSlot> hotbarSlots = new List<ItemSlot>();

    // UI �������� (��� ������ ������������� ��� �������)
    private List<GameObject> hotbarUISlots = new List<GameObject>();      // UI ������ ����
    private List<GameObject> gameHotbarUISlots = new List<GameObject>();  // UI ��� ����

    // Drag & Drop
    private int selectedSlotId = -1;
    private ItemSlot selectedItem;
    private bool isDraggingFromHotbar = false;

    private void Start()
    {
        InitializeInventoryGrid();
        InitializeHotbarUI();
        AddTestItems();
        UpdateAllUI();
    }

    private void Update()
    {
        if (selectedSlotId != -1)
            DragItem();
    }

    // ==================== ��������� ������ ��� INPUT ������� ====================

    // ����������, ����� ��������� �����������
    public void OnInventoryOpen()
    {
        UpdateAllUI();
    }

    // ����������, ����� ��������� �����������
    public void OnInventoryClose()
    {
        ClearDragSelection();
    }

    // �������� ������� ������ (�������� ��� ����������/�������� ���������)
    public void RefreshGameHotbar()
    {
        UpdateGameHotbarUI();
    }

    // �������� ������� �� ���������� ����� ������� (��� �������������)
    public ItemSlot GetSelectedHotbarItem()
    {
        // ���������� ��������� ������� (���� 0 �� ��������� ��� �� �������)
        // ���� ������ ������ ����� ����� ���� ��������
        for (int i = 0; i < hotbarSize; i++)
        {
            var outline = gameHotbarUISlots[i]?.GetComponent<Outline>();
            if (outline != null && outline.enabled)
                return hotbarSlots[i];
        }
        return null;
    }

    // ���������� ��������� ���� ������� (�� �������)
    public void SetSelectedHotbarSlot(int index)
    {
        for (int i = 0; i < gameHotbarUISlots.Count; i++)
        {
            var outline = gameHotbarUISlots[i]?.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = (i == index);
        }
    }

    // ==================== ������������� ====================

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
        // 1. ������ ������ ����
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

        // 2. ������ ��� ���� (������ UI, ������ �� hotbarSlots)
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
        // ������ ����� �� �������� ������� (��� ����)
        Debug.Log($"���� �� �������� �������, ���� {index}");
        SetSelectedHotbarSlot(index);

        // ����� ����� �������� ������������� �������� �� �������� ����� � �.�.
    }

    // ==================== UI ���������� ====================

    private void UpdateSlotUI(ItemSlot slot)
    {
        if (slot.slotObject == null) return;

        Image slotImage = slot.slotObject.GetComponent<Image>();
        Text countText = slot.slotObject.GetComponentInChildren<Text>();

        if (slot.itemId == emptyItemId || slot.count <= 0)
        {
            // ������ ���� - ���������� empty ������
            if (data != null && data.items.Count > emptyItemId)
                slotImage.sprite = data.items[emptyItemId].img;
            else
                slotImage.sprite = null;

            if (countText != null)
                countText.text = "";
        }
        else
        {
            // ���� � ���������
            if (data != null && data.items.Count > slot.itemId)
                slotImage.sprite = data.items[slot.itemId].img;

            if (countText != null)
                countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    private void UpdateGameHotbarUI()
    {
        for (int i = 0; i < hotbarSize && i < gameHotbarUISlots.Count && i < hotbarSlots.Count; i++)
        {
            var slotData = hotbarSlots[i];
            var uiObject = gameHotbarUISlots[i];

            if (uiObject == null) continue;

            Image slotImage = uiObject.GetComponent<Image>();
            Text countText = uiObject.GetComponentInChildren<Text>();

            if (slotData.itemId == emptyItemId || slotData.count <= 0)
            {
                if (data != null && data.items.Count > emptyItemId)
                    slotImage.sprite = data.items[emptyItemId].img;
                if (countText != null) countText.text = "";
            }
            else
            {
                if (data != null && data.items.Count > slotData.itemId)
                    slotImage.sprite = data.items[slotData.itemId].img;
                if (countText != null) countText.text = slotData.count > 1 ? slotData.count.ToString() : "";
            }
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
    }

    // ==================== ���������� ��������� ====================

    private void AddTestItems()
    {
        if (data == null) return;

        // ��������� �������� �������� � ���������
        for (int i = 0; i < inventorySize / 3; i++)
        {
            if (data.items.Count > 1)
            {
                int randomItem = Random.Range(1, data.items.Count);
                int randomCount = Random.Range(1, 65);
                AddItemToInventory(randomItem, randomCount);
            }
        }

        // ��������� �������� �������� � ������
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

    public bool AddItemToInventory(int itemId, int count)
    {
        // ������� ���� ������������ �����
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventorySlots[i].itemId == itemId && inventorySlots[i].count < 64)
            {
                int remaining = AddToSlot(i, SlotType.Inventory, count);
                if (remaining == 0) return true;
                count = remaining;
            }
        }

        // ���� ������ �����
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
            if (hotbarSlots[i].itemId == itemId && hotbarSlots[i].count < 64)
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

    // ==================== ������ ����������� ====================

    private void SelectSlot(int slotIndex, SlotType type)
    {
        var slot = GetSlot(type, slotIndex);

        if (selectedSlotId == -1)
        {
            // ����� �������
            if (slot.itemId != emptyItemId && slot.count > 0)
            {
                selectedSlotId = slotIndex;
                selectedItem = CopySlot(slot);
                isDraggingFromHotbar = (type == SlotType.Hotbar);

                draggingItem.gameObject.SetActive(true);
                if (data != null && data.items.Count > slot.itemId)
                    draggingItem.GetComponent<Image>().sprite = data.items[slot.itemId].img;

                ClearSlot(slot);
                UpdateSlotUI(slot);

                if (type == SlotType.Hotbar)
                    UpdateGameHotbarUI();
            }
        }
        else
        {
            // �������� �������
            if (slot.itemId == emptyItemId)
            {
                MoveToEmptySlot(slot, type);
            }
            else if (slot.itemId == selectedItem.itemId)
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
        UpdateSlotUI(targetSlot);

        var sourceSlot = GetSlot(
            isDraggingFromHotbar ? SlotType.Hotbar : SlotType.Inventory,
            selectedSlotId
        );
        sourceSlot.itemId = tempSlot.itemId;
        sourceSlot.count = tempSlot.count;
        UpdateSlotUI(sourceSlot);

        if (targetType == SlotType.Hotbar || isDraggingFromHotbar)
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

    // ==================== HELPER ������ ====================

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
    public GameObject slotObject;
    public int slotIndex;
    public SlotType type;
}