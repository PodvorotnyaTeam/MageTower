using NUnit.Framework;
using UnityEngine;

public class CampfireState : MonoBehaviour
{
    public bool isCampfireLit;
    public float timeBeforeFireGone;
    [SerializeField]
    private CampfireMehanic campfire;

    public void Awake()
    {
        isCampfireLit = false;
        timeBeforeFireGone = 180;
    }

    public void Update()
    {
        if (isCampfireLit)
        {
            timeBeforeFireGone -= Time.deltaTime;
        }

        if (timeBeforeFireGone < 0)
        {
            isCampfireLit = false;
            campfire.HardReset();
        }
    }
}
