using UnityEngine;

public class CampfireMehanic : MonoBehaviour
{
    private bool threePlanksInPlace;
    private GameObject plank1;
    private GameObject plank2;
    private GameObject plank3;
    private GameObject fire;
    private int k;
    private CampfireState state;

    public void Awake()
    {
        k = 0;
        threePlanksInPlace = false;
        plank1 = transform.GetChild(0).gameObject;
        plank2 = transform.GetChild(1).gameObject;
        plank3 = transform.GetChild(2).gameObject;
        fire = transform.GetChild(3).gameObject;
        state = GetComponent<CampfireState>();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8 && threePlanksInPlace != true)
        {
            k++;
            switch (k)
            {
                case 1:
                    plank1.SetActive(true);
                    break;
                case 2:
                    plank2.SetActive(true);
                    break;
                case 3:
                    plank3.SetActive(true);
                    threePlanksInPlace = true;
                    break;
            }
            Destroy(other.gameObject);
        }
        else if (other.gameObject.layer == 9 && threePlanksInPlace != false)
        {
            fire.SetActive(true);
            state.isCampfireLit = true;
            state.timeBeforeFireGone = 180;
        }
    }

    public void Reset()
    {
        k = 0;
        threePlanksInPlace = false;
        plank1.SetActive(!threePlanksInPlace);
        plank2.SetActive(!threePlanksInPlace);
        plank3.SetActive(!threePlanksInPlace);
        fire.SetActive(!threePlanksInPlace);
    }
}
