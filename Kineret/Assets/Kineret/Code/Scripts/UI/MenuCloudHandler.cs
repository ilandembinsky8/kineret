using DG.Tweening;
using UnityEngine;

public class MenuCloudHandler : MonoBehaviour
{
    [SerializeField] private float lapDuration;
    [SerializeField] private float lapDistance;
    private void OnEnable()
    {
        EventsRelay.OnEnableDestinationSelection += HandleEnable;

    }
    private void OnDisable()
    {
        EventsRelay.OnEnableDestinationSelection -= HandleEnable;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private void HandleEnable(bool enabled)
    {
        if (enabled) StartMoving();
    }
    private void StartMoving()
    {
        gameObject.SetActive(true);
        transform.DOMoveX(transform.position.x + lapDistance, lapDuration).SetLoops(-1,LoopType.Restart);
    }

    private void StopMoving()
    {
        gameObject.SetActive(false);
    }
}
