using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    [SerializeField] PopupData infoPopupData;

    protected override void Collect()
    {
        base.Collect();
        StartCoroutine(LoadInfoPopup(collectPopupData.Duration));
    }

    private IEnumerator LoadInfoPopup(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (infoPopupData != null)
        {
            LoadPopup_EC.RaiseEvent(infoPopupData);
        }
    }
}
