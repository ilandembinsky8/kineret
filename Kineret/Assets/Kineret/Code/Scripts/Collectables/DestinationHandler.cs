using System.Collections;
using UnityEngine;

public class DestinationHandler : CollectableHandler
{
    [SerializeField] private InfoScreenDataEventChannel LoadInfoScreen_EC;
    [SerializeField] private BoolEventChannel GamePause_EC;
    [SerializeField] private InfoScreenData infoScreenData;
    [SerializeField] private GameObject visuals;

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
        visuals.gameObject.SetActive(false);
        StartCoroutine(LoadInfoScreen(collectPopupData.Duration));
    }

    private IEnumerator LoadInfoScreen(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (infoScreenData != null)
        {
            GamePause_EC.RaiseEvent(true);
            LoadInfoScreen_EC.RaiseEvent(infoScreenData);
        }          
    } 
}
