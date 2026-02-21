using DG.Tweening;
using UnityEngine;

public class MenuCloudHandler : MonoBehaviour
{
    [SerializeField] private BoolEventChannel enableDestinationSelection_EC;
    [SerializeField] private float lapDuration;
    private void OnEnable()
    {
        enableDestinationSelection_EC.OnEventRaised += HandleEnable;


    }
    private void OnDisable()
    {
        enableDestinationSelection_EC.OnEventRaised -= HandleEnable;
    }

    private void HandleEnable(bool enabled)
    {
        if (enabled) StartMoving();
        else StopMoving();
    }
    private void StartMoving()
    {
        gameObject.SetActive(true);
        transform.DOMoveX(transform.position.x + 10000, lapDuration).SetLoops(-1,LoopType.Restart);
    }

    private void StopMoving()
    {
        gameObject.SetActive(false);
    }
}
