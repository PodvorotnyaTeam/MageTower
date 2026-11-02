using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace MageTower.Shop
{
    /// <summary>
    /// Станция взаимодействия с книгой заказов (в стиле Voices of the Void).
    /// При взаимодействии:
    /// - Фиксирует игрока и плавно перемещает камеру в точку обзора (focusPoint) перед книгой;
    /// - Проигрывает анимацию открытия книги через Animator;
    /// - Разблокирует курсор мыши и открывает интерфейс страниц заказа;
    /// - По Escape или кнопке выхода возвращает камеру назад и закрывает книгу.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BookStation : MonoBehaviour, IInteractable
    {
        public static bool IsAnyBookOpen { get; private set; }

        [Header("Точка фокусировки камеры")]
        [Tooltip("Позиция и поворот камеры перед открытой книгой")]
        [SerializeField] private Transform cameraFocusPoint;

        [Header("Аниматор книги")]
        [Tooltip("Компонент Animator книги (если пуст, ищется автоматически)")]
        [SerializeField] private Animator bookAnimator;
        [Tooltip("Имя анимации / триггера открытия")]
        [SerializeField] private string openStateName = "Open";
        [Tooltip("Имя анимации / триггера закрытия")]
        [SerializeField] private string closeStateName = "Close";

        [Header("Интерфейс страниц")]
        [Tooltip("Ссылка на Canvas / UI книги заказов")]
        [SerializeField] private OrderBookUI orderBookUI;

        [Header("Настройки перехода (VotV-style)")]
        [Tooltip("Длительность плавного перемещения камеры (сек)")]
        [SerializeField] private float transitionDuration = 0.5f;
        [Tooltip("Кривая плавности перехода камеры")]
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Ввод (New Input System)")]
        [Tooltip("Опциональное действие выхода (по умолчанию проверяется клавиша Escape на клавиатуре)")]
        [SerializeField] private InputActionReference exitAction;

        [Header("Звуки (опционально)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        // Состояние
        private bool isInteracting = false;
        private bool isTransitioning = false;

        // Сохраненное состояние игрока для возврата
        private PlayerInteractor activeInteractor;
        private PlayerMovement activeMovement;
        private CinemachineInputAxisController activeCinemachineInput;
        private Camera targetCamera;

        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;
        private Transform originalCamParent;

        private void Awake()
        {
            if (bookAnimator == null)
                bookAnimator = GetComponentInChildren<Animator>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Если точка фокуса не задана, создаем ее автоматически перед книгой
            if (cameraFocusPoint == null)
            {
                GameObject autoFocus = new GameObject("AutoCameraFocusPoint");
                autoFocus.transform.SetParent(transform);
                // Примерно 0.6м выше и 0.4м назад с наклоном вниз на книгу
                autoFocus.transform.localPosition = new Vector3(0f, 0.6f, -0.4f);
                autoFocus.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                cameraFocusPoint = autoFocus.transform;
            }

            if (orderBookUI == null)
            {
                orderBookUI = FindFirstObjectByType<OrderBookUI>(FindObjectsInactive.Include);
            }

            if (orderBookUI != null)
            {
                orderBookUI.SetStation(this);
                orderBookUI.gameObject.SetActive(false);
            }

            // Гарантируем наличие менеджера заказов на сцене
            if (OrderDeliveryManager.Instance == null)
            {
                var odm = FindFirstObjectByType<OrderDeliveryManager>();
                if (odm == null)
                {
                    GameObject odmObj = new GameObject("_OrderDeliveryManager");
                    odmObj.AddComponent<OrderDeliveryManager>();
                }
            }

            // Гарантируем корректный EventSystem для UI кликов
            OrderBookUI.EnsureEventSystem();
        }

        private void Update()
        {
            if (!isInteracting || isTransitioning) return;

            // Гарантируем свободный видимый курсор для работы с UI книги
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            // Выход из режима книги по нажатию Escape через New Input System
            bool escapeTriggered = false;

            if (exitAction != null && exitAction.action != null && exitAction.action.triggered)
            {
                escapeTriggered = true;
            }
            else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapeTriggered = true;
            }

            if (escapeTriggered)
            {
                CloseBook();
            }
        }

        /// <summary>
        /// Реализация интерфейса IInteractable.
        /// Вызывается лучом от игрока при клике/взаимодействии.
        /// </summary>
        public void Interact(PlayerInteractor interactor)
        {
            if (isTransitioning) return;

            if (isInteracting)
            {
                CloseBook();
                return;
            }

            OpenBook(interactor);
        }

        /// <summary>
        /// Вход в режим книги: фиксация игрока, перемещение камеры и открытие книги.
        /// </summary>
        public void OpenBook(PlayerInteractor interactor)
        {
            if (interactor == null) return;

            activeInteractor = interactor;
            activeMovement = interactor.GetComponent<PlayerMovement>();
            targetCamera = Camera.main;

            if (targetCamera == null)
            {
                Debug.LogError("[BookStation] Camera.main не найдена!");
                return;
            }

            // Поиск контроллера вращения Cinemachine
            activeCinemachineInput = targetCamera.GetComponentInParent<CinemachineInputAxisController>();
            if (activeCinemachineInput == null && activeMovement != null)
            {
                activeCinemachineInput = activeMovement.GetComponentInChildren<CinemachineInputAxisController>();
            }

            isInteracting = true;
            IsAnyBookOpen = true;

            // Блокируем передвижение игрока и взаимодействие лучом
            if (activeMovement != null)
            {
                activeMovement.playerVelocity = Vector3.zero;
                activeMovement.enabled = false;
            }

            if (activeInteractor != null)
                activeInteractor.enabled = false;

            if (activeCinemachineInput != null)
                activeCinemachineInput.enabled = false;

            if (exitAction != null && exitAction.action != null)
                exitAction.action.Enable();

            // Разблокируем курсор для кликов по страницам
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OrderBookUI.EnsureEventSystem();

            StartCoroutine(FocusCameraRoutine(true));
        }

        /// <summary>
        /// Выход из режима книги: закрытие анимации, плавный возврат камеры и разблокировка управления.
        /// </summary>
        public void CloseBook()
        {
            if (!isInteracting || isTransitioning) return;

            if (orderBookUI != null)
                orderBookUI.gameObject.SetActive(false);

            StartCoroutine(FocusCameraRoutine(false));
        }

        private IEnumerator FocusCameraRoutine(bool entering)
        {
            isTransitioning = true;

            Transform camTransform = targetCamera.transform;

            if (entering)
            {
                // Запоминаем исходные параметры камеры
                originalCamLocalPos = camTransform.localPosition;
                originalCamLocalRot = camTransform.localRotation;
                originalCamParent = camTransform.parent;

                // Запуск анимации открытия книги
                PlayBookAnimation(openStateName);

                if (openSound != null && audioSource != null)
                    audioSource.PlayOneShot(openSound);

                Vector3 startPos = camTransform.position;
                Quaternion startRot = camTransform.rotation;
                Vector3 endPos = cameraFocusPoint.position;
                Quaternion endRot = cameraFocusPoint.rotation;

                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

                    camTransform.position = Vector3.Lerp(startPos, endPos, t);
                    camTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
                    yield return null;
                }

                camTransform.position = endPos;
                camTransform.rotation = endRot;

                // Убеждаемся в наличии EventSystem и доступности курсора
                OrderBookUI.EnsureEventSystem();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Включаем интерфейс заказа на страницах
                if (orderBookUI != null)
                {
                    orderBookUI.gameObject.SetActive(true);
                    orderBookUI.RefreshUI();
                }
            }
            else
            {
                // Запуск анимации закрытия книги
                PlayBookAnimation(closeStateName);

                if (closeSound != null && audioSource != null)
                    audioSource.PlayOneShot(closeSound);

                Vector3 startPos = camTransform.position;
                Quaternion startRot = camTransform.rotation;

                // Целевая позиция - исходное локальное положение камеры относительно родителя
                Vector3 endPos = originalCamParent != null
                    ? originalCamParent.TransformPoint(originalCamLocalPos)
                    : originalCamLocalPos;
                Quaternion endRot = originalCamParent != null
                    ? originalCamParent.rotation * originalCamLocalRot
                    : originalCamLocalRot;

                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

                    camTransform.position = Vector3.Lerp(startPos, endPos, t);
                    camTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
                    yield return null;
                }

                camTransform.localPosition = originalCamLocalPos;
                camTransform.localRotation = originalCamLocalRot;

                // Возвращаем управление игроку
                if (activeMovement != null)
                    activeMovement.enabled = true;

                if (activeInteractor != null)
                    activeInteractor.enabled = true;

                if (activeCinemachineInput != null)
                    activeCinemachineInput.enabled = true;

                // Возвращаем фиксацию курсора в центр экрана
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (exitAction != null && exitAction.action != null)
                    exitAction.action.Disable();

                isInteracting = false;
                IsAnyBookOpen = false;
            }

            isTransitioning = false;
        }

        private void PlayBookAnimation(string stateOrTrigger)
        {
            if (bookAnimator == null || string.IsNullOrEmpty(stateOrTrigger)) return;

            // Пробуем вызвать триггер, либо проиграть состояние напрямую
            if (HasParameter(bookAnimator, stateOrTrigger, AnimatorControllerParameterType.Trigger))
            {
                bookAnimator.SetTrigger(stateOrTrigger);
            }
            else if (HasParameter(bookAnimator, "IsOpen", AnimatorControllerParameterType.Bool))
            {
                bookAnimator.SetBool("IsOpen", stateOrTrigger == openStateName);
            }
            else
            {
                // Прямой запуск состояния клипа
                bookAnimator.Play(stateOrTrigger);
            }
        }

        private bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
        {
            if (animator == null) return false;
            foreach (var p in animator.parameters)
            {
                if (p.name == paramName && p.type == type)
                    return true;
            }
            return false;
        }

        private void OnDisable()
        {
            if (isInteracting)
            {
                IsAnyBookOpen = false;
                isInteracting = false;
            }
        }
    }
}
