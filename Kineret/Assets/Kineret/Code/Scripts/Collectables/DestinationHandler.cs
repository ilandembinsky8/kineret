using System.Collections;
using UnityEngine;

public class DestinationHandler : CollectableHandler
{
    public int Destination { get; set; }

    protected override void Start()
    {
        //Overrides to not run base.Start
    }

    protected override void CheckNotifyRange(Vector3 delta)
    {
        //Overrides to not run base.CheckNotifyRange
    }

    protected override void CheckCollectRange(Vector3 delta)
    {
        if(_wasCollected) return;
        if (delta.sqrMagnitude <= _collectableData.CollectionRange * _collectableData.CollectionRange)
        {
            Collect();
        }
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
