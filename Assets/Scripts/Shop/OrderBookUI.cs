using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MageTower.Shop
{
    /// <summary>
    /// UI интерфейс страниц книги заказов.
    /// Отвечает за:
    /// - Загрузку ингредиентов из Resources/Achemy/Ingridients;
    /// - Пагинацию (перелистывание страниц через кнопки в виде согнутых уголков);
    /// - Выбор количества каждого товара (+/-);
    /// - Отображение стоимости и подсчет итоговой суммы;
    /// - Передачу сформированного заказа в OrderDeliveryManager.
    /// </summary>
    public class OrderBookUI : MonoBehaviour
    {
        [Header("Слоты товаров на развороте страниц")]
        [Tooltip("Список слотов товаров на текущих видимых страницах (например, 2 или 4 на разворот)")]
        [SerializeField] private List<BookItemSlotUI> pageSlots = new List<BookItemSlotUI>();

        [Header("Перелистывание страниц (Согнутые уголки)")]
        [Tooltip("Кнопка согнутого левого нижнего/верхнего уголка (Предыдущая страница)")]
        [SerializeField] private Button bentCornerPrevButton;
        [Tooltip("Кнопка согнутого правого нижнего/верхнего уголка (Следующая страница)")]
        [SerializeField] private Button bentCornerNextButton;
        [Tooltip("Текстовый индикатор страниц (например: Стр. 1-2 / 6)")]
        [SerializeField] private TMP_Text pageNumberText;

        [Header("Оформление и итог")]
        [Tooltip("Текст итоговой суммы заказа")]
        [SerializeField] private TMP_Text totalPriceText;
        [Tooltip("Кнопка отправки / оформления заказа")]
        [SerializeField] private Button orderButton;
        [Tooltip("Кнопка закрытия книги (также закрывается по Escape)")]
        [SerializeField] private Button closeButton;
        [Tooltip("Текст статуса / оповещения (например: Заказ оформлен!)")]
        [SerializeField] private TMP_Text statusMessageText;

        [Header("Звуки страниц (опционально)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pageFlipSound;
        [SerializeField] private AudioClip orderPlacedSound;

        // Список всех доступных ингредиентов
        private List<Ingridient> availableIngredients = new List<Ingridient>();

        // Выбранное количество по каждому ингредиенту (ID / ссылка -> количество)
        private Dictionary<Ingridient, int> cart = new Dictionary<Ingridient, int>();

        private int currentPageIndex = 0; // Индекс разворота
        private BookStation boundStation;
        private GraphicRaycaster canvasRaycaster;

        // Трекинг обработанных кликов для защиты от двойных нажатий при дублирующем рейкасте
        private static int lastHandledClickFrame = -1;

        public static void RecordClickHandled()
        {
            lastHandledClickFrame = Time.frameCount;
        }

        public void SetStation(BookStation station)
        {
            boundStation = station;
        }

        /// <summary>
        /// Гарантирует наличие EventSystem с компонентом InputSystemUIInputModule на сцене.
        /// Без этого объекта клики по UI не обрабатываются Unity UGUI при активном New Input System.
        /// </summary>
        public static void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                var uiModule = esObj.AddComponent<InputSystemUIInputModule>();
                uiModule.AssignDefaultActions();
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(esObj);
                }
                Debug.Log("[OrderBookUI] Автоматически создан EventSystem с InputSystemUIInputModule.");
            }
            else
            {
                var uiModule = es.GetComponent<InputSystemUIInputModule>();
                if (uiModule == null)
                {
                    var oldModule = es.GetComponent<StandaloneInputModule>();
                    if (oldModule != null)
                    {
                        if (Application.isPlaying)
                            Destroy(oldModule);
                        else
                            DestroyImmediate(oldModule);
                    }
                    uiModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
                    uiModule.AssignDefaultActions();
                    Debug.Log("[OrderBookUI] К существующему EventSystem прикреплен InputSystemUIInputModule.");
                }
                else
                {
                    // КРИТИЧЕСКИ ВАЖНО:
                    // Если point или leftClick не привязаны (например, m_ActionsAsset в сцене был пуст),
                    // назначаем стандартные действия принудительно!
                    if (uiModule.point == null || uiModule.point.action == null ||
                        uiModule.leftClick == null || uiModule.leftClick.action == null)
                    {
                        uiModule.AssignDefaultActions();
                        if (uiModule.isActiveAndEnabled)
                        {
                            uiModule.enabled = false;
                            uiModule.enabled = true;
                        }
                        Debug.Log("[OrderBookUI] Назначены рабочие DefaultActions (Point/Click) для InputSystemUIInputModule.");
                    }
                }
            }
        }

        private void Awake()
        {
            EnsureEventSystem();
            EnsureDefaultUIElements();
            LoadIngredientsFromResources();

            // Привязка кнопок листания (согнутые уголки)
            if (bentCornerPrevButton != null)
                bentCornerPrevButton.onClick.AddListener(OnPrevPageClicked);

            if (bentCornerNextButton != null)
                bentCornerNextButton.onClick.AddListener(OnNextPageClicked);

            if (orderButton != null)
                orderButton.onClick.AddListener(OnOrderButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnEnable()
        {
            EnsureEventSystem();
            if (statusMessageText != null)
                statusMessageText.text = "";

            RefreshUI();
        }

        private void LateUpdate()
        {
            HandleDirectClickFallback();
        }

        /// <summary>
        /// Резервный прямой перехват кликов мыши через GraphicRaycaster.
        /// Если EventSystem / InputSystemUIInputModule по какой-либо причине не передал клик в кнопку,
        /// этот метод гарантированно находит кнопку под курсором и вызывает её onClick.
        /// </summary>
        private static bool UiModuleIsUsable()
        {
            var es = EventSystem.current;

            if (es == null || !es.isActiveAndEnabled)
                return false;

            var module = es.currentInputModule as InputSystemUIInputModule;

            return module != null && module.isActiveAndEnabled
                && module.point != null && module.point.action != null
                && module.leftClick != null && module.leftClick.action != null;
        }

        private void HandleDirectClickFallback()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            // The normal UI path (EventSystem + InputSystemUIInputModule) clicks on release. Running
            // this fallback as well (it clicks on press) fired every button twice, so it only works
            // when that path is not available.
            if (UiModuleIsUsable())
                return;

            // Если клик уже был обработан в этом кадре (через EventSystem), дублировать не нужно
            if (lastHandledClickFrame == Time.frameCount)
                return;

            if (canvasRaycaster == null)
                canvasRaycaster = GetComponent<GraphicRaycaster>();

            if (canvasRaycaster == null || !canvasRaycaster.isActiveAndEnabled)
                return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = mousePos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            canvasRaycaster.Raycast(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == null) continue;

                Button button = result.gameObject.GetComponentInParent<Button>();
                if (button != null && button.interactable && button.isActiveAndEnabled)
                {
                    RecordClickHandled();
                    button.onClick.Invoke();
                    break;
                }
            }
        }

        /// <summary>
        /// Автоматическая загрузка ScriptableObject ингредиентов из Resources.
        /// Исключает устаревшие файлы из папки _OLD.
        /// </summary>
        public void LoadIngredientsFromResources()
        {
            var loaded = Resources.LoadAll<Ingridient>("Achemy/Ingridients");

            availableIngredients.Clear();
            foreach (var ing in loaded)
            {
                if (ing == null) continue;
                // Игнорируем тестовые и старые рецепты из _OLD
                if (ing.name.Contains("_OLD") || (ing.ingredientPrefab != null && ing.ingredientPrefab.name.Contains("_OLD")))
                    continue;

                availableIngredients.Add(ing);
                if (!cart.ContainsKey(ing))
                    cart[ing] = 0;
            }

            // Сортировка по имени для аккуратного каталога
            availableIngredients = availableIngredients
                .OrderBy(i => string.IsNullOrEmpty(i.nameIngridient) ? i.name : i.nameIngridient)
                .ToList();
        }

        /// <summary>
        /// Обновить отображение разворота страниц, слотов и итоговой суммы.
        /// </summary>
        public void RefreshUI()
        {
            int slotsCount = pageSlots != null && pageSlots.Count > 0 ? pageSlots.Count : 4;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)availableIngredients.Count / slotsCount));

            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);

            int startIndex = currentPageIndex * slotsCount;

            for (int i = 0; i < pageSlots.Count; i++)
            {
                int itemIndex = startIndex + i;
                if (itemIndex < availableIngredients.Count)
                {
                    Ingridient ing = availableIngredients[itemIndex];
                    pageSlots[i].Bind(ing, cart.ContainsKey(ing) ? cart[ing] : 0, OnItemCountChanged);
                    pageSlots[i].SetActive(true);
                }
                else
                {
                    pageSlots[i].SetActive(false);
                }
            }

            // Настройка кнопок уголков
            if (bentCornerPrevButton != null)
                bentCornerPrevButton.interactable = currentPageIndex > 0;

            if (bentCornerNextButton != null)
                bentCornerNextButton.interactable = currentPageIndex < totalPages - 1;

            if (pageNumberText != null)
                pageNumberText.text = $"Стр. {currentPageIndex + 1} / {totalPages}";

            UpdateTotalCost();
        }

        private void OnItemCountChanged(Ingridient ing, int newCount)
        {
            if (ing == null) return;
            cart[ing] = Mathf.Max(0, newCount);
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            int total = 0;
            int totalItems = 0;

            foreach (var pair in cart)
            {
                if (pair.Value > 0 && pair.Key != null)
                {
                    total += pair.Key.price * pair.Value;
                    totalItems += pair.Value;
                }
            }

            if (totalPriceText != null)
                totalPriceText.text = $"Итого: {total} зол. ({totalItems} шт.)";
        }

        private void OnPrevPageClicked()
        {
            RecordClickHandled();
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                PlayPageSound();
                RefreshUI();
            }
        }

        private void OnNextPageClicked()
        {
            RecordClickHandled();
            int slotsCount = pageSlots != null && pageSlots.Count > 0 ? pageSlots.Count : 4;
            int totalPages = Mathf.CeilToInt((float)availableIngredients.Count / slotsCount);

            if (currentPageIndex < totalPages - 1)
            {
                currentPageIndex++;
                PlayPageSound();
                RefreshUI();
            }
        }

        private void PlayPageSound()
        {
            if (pageFlipSound != null && audioSource != null)
                audioSource.PlayOneShot(pageFlipSound);
        }

        /// <summary>
        /// Нажатие на кнопку «Заказать»: формирует заказ и отправляет его в OrderDeliveryManager.
        /// </summary>
        private void OnOrderButtonClicked()
        {
            RecordClickHandled();
            List<OrderItem> orderItems = new List<OrderItem>();

            foreach (var pair in cart)
            {
                if (pair.Value > 0 && pair.Key != null)
                {
                    orderItems.Add(new OrderItem(pair.Key, pair.Value));
                }
            }

            if (orderItems.Count == 0)
            {
                if (statusMessageText != null)
                    statusMessageText.text = "Выберите хотя бы один товар!";
                return;
            }

            if (OrderDeliveryManager.Instance != null)
            {
                OrderDeliveryManager.Instance.PlaceOrder(orderItems);
            }
            else
            {
                Debug.LogError("[OrderBookUI] OrderDeliveryManager.Instance не найден на сцене!");
            }

            if (orderPlacedSound != null && audioSource != null)
                audioSource.PlayOneShot(orderPlacedSound);

            if (statusMessageText != null)
                statusMessageText.text = "Заказ отправлен! Заберите посылку у двери.";

            // Сброс корзины после успешного заказа
            foreach (var ing in availableIngredients)
            {
                cart[ing] = 0;
            }

            RefreshUI();
        }

        private void OnCloseButtonClicked()
        {
            RecordClickHandled();
            if (boundStation != null)
                boundStation.CloseBook();
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Создает базовый разворот книги, если слоты и элементы не были настроены вручную в инспекторе.
        /// </summary>
        private void EnsureDefaultUIElements()
        {
            transform.localScale = Vector3.one;
            gameObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
            }

            canvasRaycaster = GetComponent<GraphicRaycaster>();
            if (canvasRaycaster == null)
                canvasRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            canvasRaycaster.enabled = true;

            if (pageSlots != null && pageSlots.Count > 0)
                return;

            pageSlots = new List<BookItemSlotUI>();

            // Базовый фон переплета книги
            GameObject bookPanel = CreateUIElement("BookSpreadPanel", transform);
            RectTransform bookRect = bookPanel.GetComponent<RectTransform>();
            bookRect.anchorMin = new Vector2(0.5f, 0.5f);
            bookRect.anchorMax = new Vector2(0.5f, 0.5f);
            bookRect.sizeDelta = new Vector2(1000, 680);

            Image bookBg = bookPanel.AddComponent<Image>();
            bookBg.color = new Color(0.18f, 0.14f, 0.10f, 0.95f);
            bookBg.raycastTarget = false;

            // Пергаментный разворот страниц
            GameObject pagesObj = CreateUIElement("PagesBackground", bookPanel.transform);
            RectTransform pagesRect = pagesObj.GetComponent<RectTransform>();
            pagesRect.anchorMin = new Vector2(0.03f, 0.04f);
            pagesRect.anchorMax = new Vector2(0.97f, 0.96f);
            pagesRect.offsetMin = Vector2.zero;
            pagesRect.offsetMax = Vector2.zero;
            Image pagesImg = pagesObj.AddComponent<Image>();
            pagesImg.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            pagesImg.raycastTarget = false;

            // Линия корешка книги по центру
            GameObject spineObj = CreateUIElement("PageSpineDivider", pagesObj.transform);
            RectTransform spineRect = spineObj.GetComponent<RectTransform>();
            spineRect.anchorMin = new Vector2(0.495f, 0f);
            spineRect.anchorMax = new Vector2(0.505f, 1f);
            spineRect.offsetMin = Vector2.zero;
            spineRect.offsetMax = Vector2.zero;
            Image spineImg = spineObj.AddComponent<Image>();
            spineImg.color = new Color(0.6f, 0.55f, 0.45f, 0.7f);
            spineImg.raycastTarget = false;

            CreateTextElement("TitleLeft", pagesObj.transform, "Каталог Ингредиентов", new Vector2(300, 40), new Vector2(0.25f, 0.92f), 22, true);
            CreateTextElement("TitleRight", pagesObj.transform, "Каталог Ингредиентов", new Vector2(300, 40), new Vector2(0.75f, 0.92f), 22, true);

            Vector2[] slotAnchors = new Vector2[]
            {
                new Vector2(0.25f, 0.65f),
                new Vector2(0.25f, 0.35f),
                new Vector2(0.75f, 0.65f),
                new Vector2(0.75f, 0.35f)
            };

            for (int i = 0; i < 4; i++)
            {
                pageSlots.Add(CreateSlotUI($"ItemSlot_{i + 1}", pagesObj.transform, slotAnchors[i]));
            }

            bentCornerPrevButton = CreateCornerButton("BentCorner_Prev", pagesObj.transform, new Vector2(0.08f, 0.06f), "< Назад (Стр.)");
            bentCornerNextButton = CreateCornerButton("BentCorner_Next", pagesObj.transform, new Vector2(0.92f, 0.06f), "Вперед (Стр.) >");

            pageNumberText = CreateTextElement("PageIndicator", pagesObj.transform, "Стр. 1 / 1", new Vector2(160, 30), new Vector2(0.5f, 0.06f), 18, false);

            totalPriceText = CreateTextElement("TotalCostText", bookPanel.transform, "Итого: 0 зол.", new Vector2(300, 35), new Vector2(0.5f, -0.04f), 22, true);
            totalPriceText.color = new Color(1f, 0.9f, 0.3f, 1f);

            orderButton = CreateActionButton("Button_Order", bookPanel.transform, new Vector2(0.85f, -0.04f), "Заказать", new Color(0.2f, 0.55f, 0.2f));
            closeButton = CreateActionButton("Button_Close", bookPanel.transform, new Vector2(0.15f, -0.04f), "Закрыть (Esc)", new Color(0.5f, 0.2f, 0.2f));

            statusMessageText = CreateTextElement("StatusMessage", bookPanel.transform, "", new Vector2(500, 30), new Vector2(0.5f, -0.09f), 18, false);
            statusMessageText.color = new Color(0.4f, 1f, 0.4f);
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

            GameObject iconObj = CreateUIElement("Icon", slotObj.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.5f);
            iconRect.anchorMax = new Vector2(0.05f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(70, 70);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            TMP_Text nameText = CreateTextElement("Name", slotObj.transform, "Ингредиент", new Vector2(250, 35), new Vector2(0.6f, 0.75f), 20, true);
            nameText.alignment = TextAlignmentOptions.Left;

            TMP_Text priceText = CreateTextElement("Price", slotObj.transform, "10 зол.", new Vector2(250, 25), new Vector2(0.6f, 0.5f), 16, false);
            priceText.alignment = TextAlignmentOptions.Left;
            priceText.color = new Color(0.4f, 0.25f, 0.05f);

            GameObject counterObj = CreateUIElement("CounterGroup", slotObj.transform);
            RectTransform counterRect = counterObj.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.6f, 0.22f);
            counterRect.anchorMax = new Vector2(0.6f, 0.22f);
            counterRect.sizeDelta = new Vector2(160, 36);

            Button btnMinus = CreateSmallButton("BtnMinus", counterObj.transform, new Vector2(-55, 0), "-");
            TMP_Text countText = CreateTextElement("CountText", counterObj.transform, "0", new Vector2(50, 30), new Vector2(0.5f, 0.5f), 20, true);
            countText.alignment = TextAlignmentOptions.Center;
            Button btnPlus = CreateSmallButton("BtnPlus", counterObj.transform, new Vector2(55, 0), "+");

            return new BookItemSlotUI
            {
                rootObject = slotObj,
                iconImage = iconImg,
                nameText = nameText,
                priceText = priceText,
                countText = countText,
                buttonPlus = btnPlus,
                buttonMinus = btnMinus
            };
        }

        private static Button CreateCornerButton(string name, Transform parent, Vector2 anchor, string label)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(140, 45);

            Color baseColor = new Color(0.78f, 0.70f, 0.55f, 1f);
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
            tmp.raycastTarget = false; // Важно: текст не должен поглощать луч GraphicRaycaster вместо кнопки!
            return tmp;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.layer = LayerMask.NameToLayer("UI");
            obj.transform.SetParent(parent, false);
            return obj;
        }
    }

    /// <summary>
    /// Вспомогательный класс слота товара на странице книги.
    /// Привязывает поля отображения и кнопки +/-.
    /// </summary>
    [Serializable]
    public class BookItemSlotUI
    {
        [Tooltip("Корневой объект слота (для включения/выключения)")]
        public GameObject rootObject;
        public Image iconImage;
        public TMP_Text nameText;
        public TMP_Text priceText;
        public TMP_Text countText;
        public Button buttonPlus;
        public Button buttonMinus;

        private Ingridient boundIngredient;
        private int currentCount = 0;
        private Action<Ingridient, int> onCountChanged;

        public void Bind(Ingridient ingredient, int initialCount, Action<Ingridient, int> callback)
        {
            boundIngredient = ingredient;
            currentCount = initialCount;
            onCountChanged = callback;

            if (nameText != null)
            {
                string displayName = !string.IsNullOrEmpty(ingredient.nameIngridient)
                    ? ingredient.nameIngridient
                    : ingredient.name;
                nameText.text = displayName;
            }

            if (priceText != null)
            {
                priceText.text = $"{ingredient.price} зол.";
            }

            if (iconImage != null)
            {
                if (ingredient.icon != null)
                {
                    iconImage.sprite = ingredient.icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            UpdateCountUI();

            if (buttonPlus != null)
            {
                buttonPlus.onClick.RemoveAllListeners();
                buttonPlus.onClick.AddListener(() =>
                {
                    OrderBookUI.RecordClickHandled();
                    currentCount++;
                    UpdateCountUI();
                    onCountChanged?.Invoke(boundIngredient, currentCount);
                });
            }

            if (buttonMinus != null)
            {
                buttonMinus.onClick.RemoveAllListeners();
                buttonMinus.onClick.AddListener(() =>
                {
                    OrderBookUI.RecordClickHandled();
                    if (currentCount > 0)
                    {
                        currentCount--;
                        UpdateCountUI();
                        onCountChanged?.Invoke(boundIngredient, currentCount);
                    }
                });
            }
        }

        private void UpdateCountUI()
        {
            if (countText != null)
                countText.text = currentCount.ToString();
        }

        public void SetActive(bool active)
        {
            if (rootObject != null)
                rootObject.SetActive(active);
        }
    }
}
