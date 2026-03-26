using System.Collections;
using UnityEngine;

public class DestinationHandler : InfoPointHandler
{
    public int Destination { get; set; }

    protected override void HandleLegStart(int leg)
    {
        _isActive = leg == Leg;
    }

    protected override void CheckCollectRange(Vector3 delta)
    {
        if(_wasCollected) return;
        if (delta.sqrMagnitude <= _collectableData.CollectionRange * _collectableData.CollectionRange)
        {
            Collect();
        }
    }

    //Override for it to do nothing
    protected override void UpdateVisual(Sprite sprite)
    {
    }

    protected override void Collect()
    {
        base.Collect(); 
        FlagHandler.PlayFlagAnimation.Invoke();
        EventsRelay.OnGamePause.Invoke(true);
        StartCoroutine(DestinationReachedCoroutine(_collectPopupData.PopupTextData.Duration));   
    }

    private IEnumerator DestinationReachedCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        EventsRelay.OnDestinationReached.Invoke(Destination);        
    } 
}
