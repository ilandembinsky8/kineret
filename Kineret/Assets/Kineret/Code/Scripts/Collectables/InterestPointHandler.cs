using System.Collections;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    [SerializeField] protected PopupData infoPopupData;

    public void Init(InterestPointData interestPoint, InfoCollectableData collectableData)
    {
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        InitPopup(ref infoPopupData, collectableData.InfoPopup);
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.InfoText;
        infoPopupData.IconSprite = interestPoint.Icon;
        _collectPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.CollectText;
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
