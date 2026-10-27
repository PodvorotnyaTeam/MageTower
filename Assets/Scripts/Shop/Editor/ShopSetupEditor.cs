#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MageTower.Shop.Editor
{
    /// <summary>
    /// Утилита автоматической сборки и настройки магазина и доставки в MainScene.
    /// Доступно через меню Unity: MageTower -> Setup Shop & Delivery in MainScene.
    /// </summary>
    public static class ShopSetupEditor
    {
        [MenuItem("MageTower/Setup Shop & Delivery in MainScene")]
        public static void SetupShopInScene()
        {
            // 1. Проверяем или открываем сцену MainScene
            string mainScenePath = "Assets/Scenes/MainScene.unity";
            var currentScene = EditorSceneManager.GetActiveScene();

            if (currentScene.path != mainScenePath)
            {
                if (EditorUtility.DisplayDialog("Сменить сцену?", "Открыть MainScene для настройки?", "Да", "Отмена"))
                {
                    EditorSceneManager.OpenScene(mainScenePath);
                }
                else
                {
                    return;
                }
            }

            // 2. Ищем или создаем OrderDeliveryManager
            OrderDeliveryManager deliveryManager = Object.FindFirstObjectByType<OrderDeliveryManager>();
            if (deliveryManager == null)
            {
                GameObject systemsObj = GameObject.Find("_Systems");
                GameObject managerObj = new GameObject("OrderDeliveryManager");
                if (systemsObj != null)
                    managerObj.transform.SetParent(systemsObj.transform);

                deliveryManager = managerObj.AddComponent<OrderDeliveryManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create OrderDeliveryManager");
                Debug.Log("[ShopSetup] Создан OrderDeliveryManager.");
            }

            // Гарантируем наличие EventSystem с InputSystemUIInputModule для New Input System
            OrderBookUI.EnsureEventSystem();

            // 3. Настройка книги (Book)
            GameObject bookObj = GameObject.Find("Book");
            if (bookObj == null)
            {
                GameObject shopObj = GameObject.Find("_Shop");
                if (shopObj != null)
                {
                    Transform t = shopObj.transform.Find("Book");
                    if (t != null) bookObj = t.gameObject;
                }
            }

            if (bookObj == null)
            {
                Debug.LogError("[ShopSetup] Объект 'Book' не найден в сцене!");
                return;
            }

            // Добавляем коллайдер, если нет
            BoxCollider bookCol = bookObj.GetComponent<BoxCollider>();
            if (bookCol == null)
            {
                bookCol = bookObj.AddComponent<BoxCollider>();
                bookCol.center = new Vector3(0.3f, 0.05f, 0f);
                bookCol.size = new Vector3(0.7f, 0.15f, 0.8f);
                Debug.Log("[ShopSetup] Добавлен BoxCollider на объект Book.");
            }

            // Добавляем BookStation
            BookStation bookStation = bookObj.GetComponent<BookStation>();
            if (bookStation == null)
            {
                bookStation = bookObj.AddComponent<BookStation>();
                Debug.Log("[ShopSetup] Добавлен BookStation на объект Book.");
            }

            // Точка фокусировки камеры перед книгой
            Transform focusPoint = bookObj.transform.Find("CameraFocusPoint");
            if (focusPoint == null)
            {
                GameObject focusObj = new GameObject("CameraFocusPoint");
                focusObj.transform.SetParent(bookObj.transform);
                focusObj.transform.localPosition = new Vector3(0.3f, 0.65f, -0.45f);
                focusObj.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
                focusPoint = focusObj.transform;
            }

            // 4. Поиск или создание OrderBookCanvas
            OrderBookUI orderBookUI = Object.FindFirstObjectByType<OrderBookUI>(FindObjectsInactive.Include);
            if (orderBookUI == null)
            {
                orderBookUI = CreateOrderBookCanvas(bookObj);
            }

            // Связываем BookStation с focusPoint и UI
            var soStation = new SerializedObject(bookStation);
            soStation.FindProperty("cameraFocusPoint").objectReferenceValue = focusPoint;
            soStation.FindProperty("orderBookUI").objectReferenceValue = orderBookUI;
            soStation.ApplyModifiedProperties();

            // 5. Настройка двери (DeliveryDoor)
            SetupDeliveryDoor();

            // Помечаем сцену измененной
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[ShopSetup] Настройка магазина и доставки в MainScene успешно завершена!");
        }

        private static OrderBookUI CreateOrderBookCanvas(GameObject bookObj)
        {
            GameObject canvasObj = new GameObject("OrderBookCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Корневая панель разворота книги
            GameObject bookPanel = CreateUIElement("BookSpreadPanel", canvasObj.transform);
            RectTransform bookRect = bookPanel.GetComponent<RectTransform>();
            bookRect.anchorMin = new Vector2(0.5f, 0.5f);
            bookRect.anchorMax = new Vector2(0.5f, 0.5f);
            bookRect.sizeDelta = new Vector2(1000, 680);

            Image bookBg = bookPanel.AddComponent<Image>();
            bookBg.color = new Color(0.18f, 0.14f, 0.10f, 0.95f); // Кожаный переплет
            bookBg.raycastTarget = false;

            // Внутренний пергамент (разворот страниц)
            GameObject pagesObj = CreateUIElement("PagesBackground", bookPanel.transform);
            RectTransform pagesRect = pagesObj.GetComponent<RectTransform>();
            pagesRect.anchorMin = new Vector2(0.03f, 0.04f);
            pagesRect.anchorMax = new Vector2(0.97f, 0.96f);
            pagesRect.offsetMin = Vector2.zero;
            pagesRect.offsetMax = Vector2.zero;
            Image pagesImg = pagesObj.AddComponent<Image>();
            pagesImg.color = new Color(0.92f, 0.88f, 0.78f, 1f); // Цвет пергамента
            pagesImg.raycastTarget = false;

            // Разделитель страниц по центру
            GameObject spineObj = CreateUIElement("PageSpineDivider", pagesObj.transform);
            RectTransform spineRect = spineObj.GetComponent<RectTransform>();
            spineRect.anchorMin = new Vector2(0.495f, 0f);
            spineRect.anchorMax = new Vector2(0.505f, 1f);
            spineRect.offsetMin = Vector2.zero;
            spineRect.offsetMax = Vector2.zero;
            Image spineImg = spineObj.AddComponent<Image>();
            spineImg.color = new Color(0.6f, 0.55f, 0.45f, 0.7f);
            spineImg.raycastTarget = false;

            // Заголовок книги
            CreateTextElement("TitleLeft", pagesObj.transform, "Книга Заказов: Травы", new Vector2(250, 40), new Vector2(0.25f, 0.92f), 24, true);
            CreateTextElement("TitleRight", pagesObj.transform, "Книга Заказов: Минералы", new Vector2(250, 40), new Vector2(0.75f, 0.92f), 24, true);

            // Создаем 4 слота товаров (2 слева, 2 справа)
            List<BookItemSlotUI> slotList = new List<BookItemSlotUI>();
            Vector2[] slotAnchors = new Vector2[]
            {
                new Vector2(0.25f, 0.65f), // Слот 1 (Левый верх)
                new Vector2(0.25f, 0.35f), // Слот 2 (Левый низ)
                new Vector2(0.75f, 0.65f), // Слот 3 (Правый верх)
                new Vector2(0.75f, 0.35f)  // Слот 4 (Правый низ)
            };

            for (int i = 0; i < 4; i++)
            {
                slotList.Add(CreateSlotUI($"ItemSlot_{i + 1}", pagesObj.transform, slotAnchors[i]));
            }

            // Кнопка согнутого левого нижнего уголка (Предыдущая страница)
            Button prevCornerBtn = CreateCornerButton("BentCorner_Prev", pagesObj.transform, new Vector2(0.04f, 0.05f), "< Назад (Стр.)");

            // Кнопка согнутого правого нижнего уголка (Следующая страница)
            Button nextCornerBtn = CreateCornerButton("BentCorner_Next", pagesObj.transform, new Vector2(0.96f, 0.05f), "Вперед (Стр.) >");

            // Индикатор страниц по центру внизу
            TMP_Text pageIndicator = CreateTextElement("PageIndicator", pagesObj.transform, "Стр. 1 / 4", new Vector2(160, 30), new Vector2(0.5f, 0.06f), 18, false);

            // Текст итоговой суммы
            TMP_Text totalText = CreateTextElement("TotalCostText", bookPanel.transform, "Итого: 0 зол.", new Vector2(300, 35), new Vector2(0.5f, -0.04f), 22, true);
            totalText.color = new Color(1f, 0.9f, 0.3f, 1f);

            // Кнопка «Заказать»
            Button orderBtn = CreateActionButton("Button_Order", bookPanel.transform, new Vector2(0.85f, -0.04f), "Заказать", new Color(0.2f, 0.55f, 0.2f));

            // Кнопка «Закрыть»
            Button closeBtn = CreateActionButton("Button_Close", bookPanel.transform, new Vector2(0.15f, -0.04f), "Закрыть (Esc)", new Color(0.5f, 0.2f, 0.2f));

            // Текст оповещений
            TMP_Text statusText = CreateTextElement("StatusMessage", bookPanel.transform, "", new Vector2(500, 30), new Vector2(0.5f, -0.09f), 18, false);
            statusText.color = new Color(0.4f, 1f, 0.4f);

            // Навешиваем компонент OrderBookUI
            OrderBookUI orderBookUI = canvasObj.AddComponent<OrderBookUI>();
            var so = new SerializedObject(orderBookUI);

            SerializedProperty pageSlotsProp = so.FindProperty("pageSlots");
            pageSlotsProp.ClearArray();
            for (int i = 0; i < slotList.Count; i++)
            {
                pageSlotsProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = pageSlotsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("rootObject").objectReferenceValue = slotList[i].rootObject;
                elem.FindPropertyRelative("iconImage").objectReferenceValue = slotList[i].iconImage;
                elem.FindPropertyRelative("nameText").objectReferenceValue = slotList[i].nameText;
                elem.FindPropertyRelative("priceText").objectReferenceValue = slotList[i].priceText;
                elem.FindPropertyRelative("countText").objectReferenceValue = slotList[i].countText;
                elem.FindPropertyRelative("buttonPlus").objectReferenceValue = slotList[i].buttonPlus;
                elem.FindPropertyRelative("buttonMinus").objectReferenceValue = slotList[i].buttonMinus;
            }

            so.FindProperty("bentCornerPrevButton").objectReferenceValue = prevCornerBtn;
            so.FindProperty("bentCornerNextButton").objectReferenceValue = nextCornerBtn;
            so.FindProperty("pageNumberText").objectReferenceValue = pageIndicator;
            so.FindProperty("totalPriceText").objectReferenceValue = totalText;
            so.FindProperty("orderButton").objectReferenceValue = orderBtn;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("statusMessageText").objectReferenceValue = statusText;
            so.ApplyModifiedProperties();

            canvasObj.SetActive(false);
            return orderBookUI;
        }

        private static BookItemSlotUI CreateSlotUI(string name, Transform parent, Vector2 anchorCenter)
        {
            GameObject slotObj = CreateUIElement(name, parent);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchorMin = anchorCenter;
            slotRect.anchorMax = anchorCenter;
            slotRect.sizeDelta = new Vector2(380, 160);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.85f, 0.80f, 0.68f, 0.7f);
            slotBg.raycastTarget = false;

            // Иконка
            GameObject iconObj = CreateUIElement("Icon", slotObj.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.5f);
            iconRect.anchorMax = new Vector2(0.05f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(70, 70);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            // Название
            TMP_Text nameText = CreateTextElement("Name", slotObj.transform, "Ингредиент", new Vector2(250, 35), new Vector2(0.6f, 0.75f), 20, true);
            nameText.alignment = TextAlignmentOptions.Left;

            // Цена
            TMP_Text priceText = CreateTextElement("Price", slotObj.transform, "10 зол.", new Vector2(250, 25), new Vector2(0.6f, 0.5f), 16, false);
            priceText.alignment = TextAlignmentOptions.Left;
            priceText.color = new Color(0.4f, 0.25f, 0.05f);

            // Панель счетчика: [-] [Count] [+]
            GameObject counterObj = CreateUIElement("CounterGroup", slotObj.transform);
            RectTransform counterRect = counterObj.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.6f, 0.22f);
            counterRect.anchorMax = new Vector2(0.6f, 0.22f);
            counterRect.sizeDelta = new Vector2(160, 36);

            Button btnMinus = CreateSmallButton("BtnMinus", counterObj.transform, new Vector2(-55, 0), "-");
            TMP_Text countText = CreateTextElement("CountText", counterObj.transform, "0", new Vector2(50, 30), new Vector2(0.5f, 0.5f), 20, true);
            countText.alignment = TextAlignmentOptions.Center;
            Button btnPlus = CreateSmallButton("BtnPlus", counterObj.transform, new Vector2(55, 0), "+");

            BookItemSlotUI slotUI = new BookItemSlotUI
            {
                rootObject = slotObj,
                iconImage = iconImg,
                nameText = nameText,
                priceText = priceText,
                countText = countText,
                buttonPlus = btnPlus,
                buttonMinus = btnMinus
            };

            return slotUI;
        }

        private static Button CreateCornerButton(string name, Transform parent, Vector2 anchor, string label)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(140, 45);

            Color baseColor = new Color(0.78f, 0.70f, 0.55f, 1f); // Эффект загнутого бумажного уголка
            Image img = btnObj.AddComponent<Image>();
            img.color = baseColor;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock cb = btn.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.35f);
            cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.3f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            btn.colors = cb;

            TMP_Text txt = CreateTextElement("Text", btnObj.transform, label, new Vector2(130, 35), new Vector2(0.5f, 0.5f), 14, true);
            txt.color = new Color(0.25f, 0.18f, 0.10f);
            txt.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static Button CreateActionButton(string name, Transform parent, Vector2 anchor, string label, Color color)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(160, 45);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.3f);
            cb.pressedColor = Color.Lerp(color, Color.black, 0.35f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(color.r, color.g, color.b, 0.4f);
            btn.colors = cb;

            TMP_Text txt = CreateTextElement("Text", btnObj.transform, label, new Vector2(150, 35), new Vector2(0.5f, 0.5f), 18, true);
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static Button CreateSmallButton(string name, Transform parent, Vector2 anchoredPos, string label)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(36, 32);

            Color baseColor = new Color(0.4f, 0.3f, 0.2f);
            Image img = btnObj.AddComponent<Image>();
            img.color = baseColor;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock cb = btn.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.35f);
            cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.35f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            btn.colors = cb;

            TMP_Text txt = CreateTextElement("Text", btnObj.transform, label, new Vector2(32, 28), new Vector2(0.5f, 0.5f), 18, true);
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static TMP_Text CreateTextElement(string name, Transform parent, string content, Vector2 size, Vector2 anchor, float fontSize, bool bold)
        {
            GameObject textObj = CreateUIElement(name, parent);
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;

            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = new Color(0.15f, 0.10f, 0.05f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // Важно: предотвращает блокировку кликов по кнопкам текстовым слоем
            return tmp;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.layer = LayerMask.NameToLayer("UI");
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetupDeliveryDoor()
        {
            // Ищем объект двери или точку ExitPoint
            GameObject doorObj = GameObject.Find("Door");
            if (doorObj == null)
            {
                GameObject exitPoint = GameObject.Find("ExitPoint");
                if (exitPoint != null)
                {
                    doorObj = new GameObject("DeliveryDoor");
                    doorObj.transform.position = exitPoint.transform.position;
                    doorObj.transform.rotation = exitPoint.transform.rotation;
                    BoxCollider col = doorObj.AddComponent<BoxCollider>();
                    col.size = new Vector2(2f, 3f);
                }
            }

            if (doorObj == null)
            {
                Debug.LogWarning("[ShopSetup] Не найден объект двери или ExitPoint.");
                return;
            }

            DeliveryDoor door = doorObj.GetComponent<DeliveryDoor>();
            if (door == null)
            {
                door = doorObj.AddComponent<DeliveryDoor>();
                Debug.Log("[ShopSetup] Добавлен DeliveryDoor на " + doorObj.name);
            }

            // Создаем два пивота
            Transform pivotA = doorObj.transform.Find("DeliveryPivot_A");
            if (pivotA == null)
            {
                GameObject pA = new GameObject("DeliveryPivot_A");
                pA.transform.SetParent(doorObj.transform);
                pA.transform.localPosition = new Vector3(-1.2f, 0f, 0.8f);
                pivotA = pA.transform;
            }

            Transform pivotB = doorObj.transform.Find("DeliveryPivot_B");
            if (pivotB == null)
            {
                GameObject pB = new GameObject("DeliveryPivot_B");
                pB.transform.SetParent(doorObj.transform);
                pB.transform.localPosition = new Vector3(1.2f, 0f, 0.8f);
                pivotB = pB.transform;
            }

            GameObject boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Misc/DeliveryBox.prefab");
            if (boxPrefab == null)
            {
                boxPrefab = Resources.Load<GameObject>("Shop/DeliveryBox");
            }

            var soDoor = new SerializedObject(door);
            soDoor.FindProperty("pivotPointA").objectReferenceValue = pivotA;
            soDoor.FindProperty("pivotPointB").objectReferenceValue = pivotB;
            if (boxPrefab != null)
                soDoor.FindProperty("deliveryBoxPrefab").objectReferenceValue = boxPrefab;
            soDoor.ApplyModifiedProperties();
        }
    }
}
#endif
