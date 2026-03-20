using System.Collections;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    [SerializeField] protected PopupData infoPopupData;

    public void Init(InterestPointData interestPoint, InfoCollectableData collectableData)
    {
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        infoPopupData.IconSprite = interestPoint.Icon;

        infoPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.InfoText.HebText;   
        _collectPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.CollectText.HebText;
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
