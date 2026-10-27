using System;
using System.Collections.Generic;
using UnityEngine;

namespace MageTower.Shop
{
    /// <summary>
    /// Центральный менеджер заказов и доставки.
    /// Хранит список купленных товаров до момента получения у двери.
    /// </summary>
    public class OrderDeliveryManager : MonoBehaviour
    {
        public static OrderDeliveryManager Instance { get; private set; }

        [Header("Ожидающие доставки товары")]
        [SerializeField] private List<OrderItem> pendingItems = new List<OrderItem>();

        /// <summary>
        /// Событие оформления нового заказа (передает полный актуальный список)
        /// </summary>
        public event Action<List<OrderItem>> OnOrderPlaced;

        /// <summary>
        /// Событие успешной доставки / получения коробки
        /// </summary>
        public event Action OnOrderDelivered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Оформить новый заказ (вызывается из интерфейса книги).
        /// </summary>
        public void PlaceOrder(List<OrderItem> items)
        {
            if (items == null || items.Count == 0) return;

            foreach (var item in items)
            {
                if (item.ingredient == null || item.count <= 0) continue;

                // Если ингредиент уже был в списке ожидания, увеличиваем количество
                var existing = pendingItems.Find(p => p.ingredient == item.ingredient);
                if (existing != null)
                {
                    existing.count += item.count;
                }
                else
                {
                    pendingItems.Add(new OrderItem(item.ingredient, item.count));
                }
            }

            Debug.Log($"[OrderDeliveryManager] Заказ успешно принят! Всего наименований: {pendingItems.Count}");
            OnOrderPlaced?.Invoke(new List<OrderItem>(pendingItems));
        }

        /// <summary>
        /// Проверка: есть ли заказ, готовый к выдаче у двери?
        /// </summary>
        public bool HasPendingOrder()
        {
            return pendingItems != null && pendingItems.Count > 0;
        }

        /// <summary>
        /// Извлечь все ожидающие товары и очистить корзину доставки (передается в коробку).
        /// </summary>
        public List<OrderItem> TakePendingOrder()
        {
            var result = new List<OrderItem>(pendingItems);
            pendingItems.Clear();

            Debug.Log($"[OrderDeliveryManager] Заказ выдан у двери. Передано позиций: {result.Count}");
            OnOrderDelivered?.Invoke();
            return result;
        }

        /// <summary>
        /// Просмотр текущих позиций без извлечения
        /// </summary>
        public IReadOnlyList<OrderItem> GetPendingItems() => pendingItems;
    }
}
