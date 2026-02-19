using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlagHandler : MonoBehaviour
{
    public static UnityAction PlayFlagAnimation;
    public static UnityAction EndFlagAnimation;

    [SerializeField] private Animator flagAnimator;

    private void OnEnable()
    {
        PlayFlagAnimation += PlayAnimation;
        EndFlagAnimation += PlayAnimation;
    }
    private void OnDisable()
    {
        PlayFlagAnimation -= PlayAnimation;
        EndFlagAnimation -= PlayAnimation;
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
