using UnityEngine;

namespace MageTower.Shop
{
    /// <summary>
    /// Интерактивная дверь для получения заказов.
    /// При взаимодействии:
    /// - Проверяет наличие ожидающих заказов в OrderDeliveryManager;
    /// - Выбирает один из двух пивотов спавна (тот, где сейчас НЕ находится игрок);
    /// - Спавнит коробку с купленными товарами.
    /// </summary>
    public class DeliveryDoor : MonoBehaviour, IInteractable
    {
        [Header("Точки спавна коробки (Pivots)")]
        [Tooltip("Первая точка спавна коробки")]
        [SerializeField] private Transform pivotPointA;
        [Tooltip("Вторая точка спавна коробки")]
        [SerializeField] private Transform pivotPointB;

        [Header("Префаб коробки доставки")]
        [Tooltip("Префаб коробки (DeliveryBox)")]
        [SerializeField] private GameObject deliveryBoxPrefab;

        [Header("Звуки и эффекты (опционально)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip doorKnockSound;
        [SerializeField] private AudioClip deliveryArrivedSound;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Если пивоты не назначены, создаем автоматические точки по бокам двери
            if (pivotPointA == null)
            {
                GameObject pA = new GameObject("DeliveryPivot_A");
                pA.transform.SetParent(transform);
                pA.transform.localPosition = new Vector3(-1.0f, 0f, 0.5f);
                pivotPointA = pA.transform;
            }

            if (pivotPointB == null)
            {
                GameObject pB = new GameObject("DeliveryPivot_B");
                pB.transform.SetParent(transform);
                pB.transform.localPosition = new Vector3(1.0f, 0f, 0.5f);
                pivotPointB = pB.transform;
            }

            // Автоматическая загрузка префаба коробки из Resources при отсутствии ручной ссылки
            if (deliveryBoxPrefab == null)
            {
                deliveryBoxPrefab = Resources.Load<GameObject>("Shop/DeliveryBox");
            }
        }

        /// <summary>
        /// Реализация IInteractable. Вызывается лучом игрока при клике на дверь.
        /// </summary>
        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null) return;

            if (doorKnockSound != null && audioSource != null)
                audioSource.PlayOneShot(doorKnockSound);

            if (OrderDeliveryManager.Instance == null)
            {
                Debug.LogWarning("[DeliveryDoor] OrderDeliveryManager не найден на сцене!");
                return;
            }

            if (!OrderDeliveryManager.Instance.HasPendingOrder())
            {
                Debug.Log("[DeliveryDoor] Посылок нет! Сначала оформите заказ в книге.");
                return;
            }

            // Выбор точки спавна: определяем расстояние до обеих точек.
            // Спавним в той точке, от которой игрок находится ДАЛЬШЕ (т.е. где его сейчас нет).
            Transform playerTransform = interactor.transform;
            float distA = Vector3.Distance(playerTransform.position, pivotPointA.position);
            float distB = Vector3.Distance(playerTransform.position, pivotPointB.position);

            Transform spawnPivot = (distA >= distB) ? pivotPointA : pivotPointB;

            Debug.Log($"[DeliveryDoor] Доставка прибыла! Выбран пивот: {spawnPivot.name} (дистанция до A={distA:F1}м, до B={distB:F1}м)");

            if (deliveryBoxPrefab == null)
            {
                Debug.LogError("[DeliveryDoor] Не назначен deliveryBoxPrefab!");
                return;
            }

            // Спавним коробку
            GameObject boxObj = Instantiate(deliveryBoxPrefab, spawnPivot.position, spawnPivot.rotation);

            // Передаем в коробку список заказанных товаров из менеджера
            var pendingOrder = OrderDeliveryManager.Instance.TakePendingOrder();
            var boxComponent = boxObj.GetComponent<DeliveryBox>();

            if (boxComponent != null)
            {
                boxComponent.Initialize(pendingOrder);
            }
            else
            {
                Debug.LogWarning("[DeliveryDoor] На созданном префабе коробки отсутствует компонент DeliveryBox!");
            }

            if (deliveryArrivedSound != null && audioSource != null)
                audioSource.PlayOneShot(deliveryArrivedSound);
        }
    }
}
