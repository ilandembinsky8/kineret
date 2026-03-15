using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    [SerializeField] protected PopupData infoPopupData;

    public void Init(InterestPointSO interestPoint, InfoCollectableData collectableData)
    {
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData.Description = interestPoint.InterestPointData.InfoText;
        infoPopupData.IconSprite = interestPoint.Icon;
        _collectPopupData.PopupTextData.Description = interestPoint.InterestPointData.CollectText;
    }

    protected override void Collect()
    {
        base.Collect();
        StartCoroutine(LoadInfoPopup(_collectPopupData.PopupTextData.Duration));
    }

    private IEnumerator LoadInfoPopup(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPopup_EC.RaiseEvent(infoPopupData);
    }
}
