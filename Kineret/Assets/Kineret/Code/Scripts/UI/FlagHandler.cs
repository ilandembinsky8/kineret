using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlagHandler : MonoBehaviour
{
    public static UnityAction PlayFlagAnimation;
    public static UnityAction EndFlagAnimation;

    [SerializeField] private Animator flagAnimator;

    [SerializeField] private float animationSpeed;
    private void Awake()
    {
        flagAnimator.speed = animationSpeed;
    }

    private void OnEnable()
    {
        PlayFlagAnimation += PlayAnimation;
        EndFlagAnimation += EndAnimation;
    }
    private void OnDisable()
    {
        PlayFlagAnimation -= PlayAnimation;
        EndFlagAnimation -= EndAnimation;
    }

    private void PlayAnimation()
    {
        flagAnimator.SetTrigger("Play");
    }
    private void EndAnimation()
    {
        flagAnimator.SetTrigger("End");
    }
}
