using System.Collections;
using UnityEngine;

public class DestinationHandler : CollectableHandler
{
    [SerializeField] private InfoScreenDataEventChannel DestinationReached_EC;
    [SerializeField] private BoolEventChannel GamePause_EC;
    [SerializeField] private InfoScreenData infoScreenData;

    protected override void Start()
    {}

    protected override void CheckNotifyRange(Vector3 delta)
    {}

    protected override void CheckCollectRange(Vector3 delta)
    {

        if (delta.sqrMagnitude <= collectableData.collectionRange * collectableData.collectionRange)
        {
            Collect();
        }
    }

    protected override void Collect()
    {
        base.Collect();
        StartCoroutine(LoadInfoScreen(collectPopupData.Duration));
    }

    private IEnumerator LoadInfoScreen(float delay)
    {
        GamePause_EC.RaiseEvent(true);
        yield return new WaitForSeconds(delay);
        if (infoScreenData != null)
        {
            DestinationReached_EC.RaiseEvent(infoScreenData);
        }          
    } 
}
