using UnityEngine;

public class Bucket_behavior : MonoBehaviour, IInteractable
{
    [Header("Наклон")]
    public float tiltAngleThreshold = 45f; // угол, после которого ведро считается наклонённым
    public float rayDistance = 10f;

    [Header("Наполнение котла")]
    public float fillTime1 = 1f; // через сколько секунд налива включится 1 уровень воды
    public float fillTime2 = 2f;
    public float fillTime3 = 3f;

    private float pourTimer = 0f;
    private bool isFull;

    [SerializeField] private GameObject waterLevel1;
    [SerializeField] private GameObject waterLevel2;
    [SerializeField] private GameObject waterLevel3;
    [SerializeField] private CauldronState cauldronState;

    private IGrabbable grabbableComponent;
    private RaycastHit hit;

    public void Awake()
    {
        isFull = false;
        grabbableComponent = GetComponent<IGrabbable>();
    }

    public void Update()
    {
        bool isTilted = IsTilted();

        Debug.DrawRay(transform.position, Vector3.down * rayDistance, isTilted ? Color.green : Color.red);

        // Наливаем только пока ведро наклонено и луч бьёт в котёл
        if (!isFull && isTilted && Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance))
        {
            Debug.Log("Ray is casted");
            if (hit.collider.CompareTag("Cauldron's_bottom"))
            {
                Debug.Log("Ray hitted");
                pourTimer += Time.deltaTime;
                UpdateWaterLevels();
            }
        }
        // Если ведро выпрямили или луч ушёл мимо — просто ничего не делаем,
        // таймер и уже налитые уровни остаются как есть
    }

    private bool IsTilted()
    {
        Debug.Log("is tilted");
        float currentAngle = Vector3.Angle(transform.up, Vector3.up);
        return currentAngle >= tiltAngleThreshold;
    }

    private void UpdateWaterLevels()
    {
        if (pourTimer >= fillTime1 && waterLevel1 != null && !waterLevel1.activeSelf)
            waterLevel1.SetActive(true);

        if (pourTimer >= fillTime2 && waterLevel2 != null && !waterLevel2.activeSelf)
            waterLevel2.SetActive(true);

        if (pourTimer >= fillTime3 && waterLevel3 != null && !waterLevel3.activeSelf)
        {
            waterLevel3.SetActive(true);
            isFull = true;

            if (cauldronState != null)
                cauldronState.cauldronIsFull = true;
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(grabbableComponent);
    }

    public void ResetWaterLLevels()
    {
        waterLevel1.SetActive(false);
        waterLevel2.SetActive(false);
        waterLevel3.SetActive(false);
        cauldronState.cauldronIsFull = false;
        pourTimer = 0;
    }
}
//using Unity.VisualScripting;
//using UnityEngine;

//public class Bucket_behavior : MonoBehaviour, IInteractable
//{
//    public int angle;
//    private bool isFull;
//    [SerializeField]
//    private GameObject waterLevel1;
//    [SerializeField]
//    private GameObject waterLevel2;
//    [SerializeField]
//    private GameObject waterLevel3;
//    [SerializeField]
//    private CauldronState cauldronState;
//    private IGrabbable grabbableComponent;
//    private RaycastHit hit;

//    public void Awake()
//    {
//        isFull = false;
//        grabbableComponent = GetComponent<IGrabbable>();
//    }

//    public void Update()
//    {
//        Debug.DrawRay(transform.position, Vector3.down, Color.red, 10f);
//        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit);
//        if (hit.collider != null && hit.collider.CompareTag("Cauldron's_bottom"))
//        {
//            Debug.Log("Попали в: " + hit.collider.name);
//        }
//    }

//    public void Interact(PlayerInteractor interactor)
//    {
//        interactor.Grab(grabbableComponent);
//        Vector3 dir = Quaternion.Euler(angle, 0, 0) * Vector3.down;

//    }
//}
