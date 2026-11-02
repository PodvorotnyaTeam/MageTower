using NUnit.Framework;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CampfireState : MonoBehaviour
{
    public bool isCampfireLit;
    public float timeBeforeFireGone;
    [SerializeField]
    private CampfireMehanic campfire;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip campfireSound;

    public void Awake()
    {
        isCampfireLit = false;
        timeBeforeFireGone = 180;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (campfire == null)
        {
            campfire = GetComponent<CampfireMehanic>();
        }
    }

    public void Update()
    {
        if (isCampfireLit)
        {
            timeBeforeFireGone -= Time.deltaTime;

            if (audioSource != null)
            {
                if (campfireSound != null && audioSource.clip != campfireSound)
                {
                    audioSource.clip = campfireSound;
                }

                if (audioSource.clip != null)
                {
                    audioSource.loop = true;
                    if (!audioSource.isPlaying)
                    {
                        audioSource.Play();
                    }
                }
            }
        }
        else
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        if (timeBeforeFireGone < 0)
        {
            isCampfireLit = false;
            campfire.HardReset();

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
