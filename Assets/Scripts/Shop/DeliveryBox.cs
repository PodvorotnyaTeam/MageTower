using System.Collections.Generic;
using UnityEngine;

namespace MageTower.Shop
{
    /// <summary>
    /// Коробка с доставленными ингредиентами.
    /// Реализует IInteractable:
    /// При взаимодействии распаковывает купленные товары:
    /// - Физически выбрасывает префабы ингредиентов в мир с приятным импульсом (основной режим);
    /// - Либо (при включенной опции) пробует переместить в инвентарь игрока, а излишки при переполнении выбрасывает на землю;
    /// - После опустошения коробка исчезает (самоуничтожается).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeliveryBox : MonoBehaviour, IInteractable
    {
        [Header("Содержимое коробки")]
        [SerializeField] private List<OrderItem> storedItems = new List<OrderItem>();

        [Header("Настройки распаковки")]
        [Tooltip("Точка высыпания предметов (если не задана, берется центр коробки + смещение вверх)")]
        [SerializeField] private Transform dropPoint;
        [Tooltip("Радиус случайного разброса при выпадении предметов")]
        [SerializeField] private float dropSpreadRadius = 0.35f;
        [Tooltip("Сила импульса подбрасывания вверх")]
        [SerializeField] private float dropUpwardForce = 2.0f;

        [Header("Интеграция с инвентарем (опционально)")]
        [Tooltip("Если включено, сначала пробует положить в инвентарь игрока, а переполненные/неизвестные предметы выкидывает в мир")]
        [SerializeField] private bool tryAddToInventoryFirst = false;

        [Header("Эффекты при открытии (опционально)")]
        [SerializeField] private GameObject unpackEffectPrefab;
        [SerializeField] private AudioClip unpackSound;

        /// <summary>
        /// Инициализация коробки списком товаров из доставки
        /// </summary>
        public void Initialize(List<OrderItem> items)
        {
            storedItems = items != null ? new List<OrderItem>(items) : new List<OrderItem>();
        }

        /// <summary>
        /// Реализация IInteractable. Вызывается лучом игрока при клике на коробку.
        /// </summary>
        public void Interact(PlayerInteractor interactor)
        {
            if (storedItems == null || storedItems.Count == 0)
            {
                Debug.Log("[DeliveryBox] Коробка пуста и исчезает.");
                Destroy(gameObject);
                return;
            }

            Inventory inventory = null;
            if (tryAddToInventoryFirst)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }

            Vector3 spawnOrigin = dropPoint != null ? dropPoint.position : transform.position + Vector3.up * 0.3f;

            // Распаковываем все товары
            foreach (var orderItem in storedItems)
            {
                if (orderItem.ingredient == null || orderItem.count <= 0) continue;

                int itemsToDropPhysically = orderItem.count;

                // Если включена попытка добавления в инвентарь
                if (tryAddToInventoryFirst && inventory != null && inventory.data != null)
                {
                    // Ищем соответствующий предмет в Database по имени
                    string ingName = !string.IsNullOrEmpty(orderItem.ingredient.nameIngridient)
                        ? orderItem.ingredient.nameIngridient
                        : orderItem.ingredient.name;

                    var dbItem = inventory.data.items.Find(item =>
                        string.Equals(item.name, ingName, System.StringComparison.OrdinalIgnoreCase));

                    if (dbItem != null)
                    {
                        // Пробуем добавить в инвентарь
                        bool added = inventory.AddItemToInventory(dbItem.id, orderItem.count);
                        if (added)
                        {
                            itemsToDropPhysically = 0;
                            Debug.Log($"[DeliveryBox] {ingName} x{orderItem.count} добавлен в инвентарь.");
                        }
                        else
                        {
                            Debug.Log($"[DeliveryBox] Инвентарь переполнен! Выбрасываем {ingName} на землю.");
                        }
                    }
                }

                // Выбрасываем оставшиеся предметы в мир как физические 3D объекты
                for (int i = 0; i < itemsToDropPhysically; i++)
                {
                    SpawnPhysicalIngredient(orderItem.ingredient, spawnOrigin);
                }
            }

            // Очищаем содержимое
            storedItems.Clear();

            // Воспроизводим звук и эффекты
            if (unpackSound != null)
                AudioSource.PlayClipAtPoint(unpackSound, transform.position);

            if (unpackEffectPrefab != null)
                Instantiate(unpackEffectPrefab, spawnOrigin, Quaternion.identity);

            Debug.Log("[DeliveryBox] Коробка успешно распакована и исчезает.");

            // Коробка исчезает, так как в ней ничего не осталось
            Destroy(gameObject);
        }

        private void SpawnPhysicalIngredient(Ingridient ingredient, Vector3 origin)
        {
            if (ingredient.ingredientPrefab == null)
            {
                Debug.LogWarning($"[DeliveryBox] У ингредиента {ingredient.name} не назначен ingredientPrefab!");
                return;
            }

            Vector3 randomOffset = new Vector3(
                Random.Range(-dropSpreadRadius, dropSpreadRadius),
                Random.Range(0f, 0.2f),
                Random.Range(-dropSpreadRadius, dropSpreadRadius)
            );

            Quaternion randomRot = Random.rotation;
            GameObject spawnedObj = Instantiate(ingredient.ingredientPrefab, origin + randomOffset, randomRot);

            // Добавляем небольшой импульс физике, чтобы предметы красиво рассыпались
            Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 popVelocity = (Vector3.up * dropUpwardForce) +
                    new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                rb.linearVelocity = popVelocity;
                rb.angularVelocity = Random.insideUnitSphere * 5f;
            }
        }
    }
}
