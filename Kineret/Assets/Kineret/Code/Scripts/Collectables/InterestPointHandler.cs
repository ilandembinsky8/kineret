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

        infoPopupData.PopupTextData.TextData.HebTitle = interestPoint.Data.Name.HebText;
        infoPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.InfoText.HebText;   
        _collectPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.CollectText.HebText;
        infoPopupData.PopupTextData.TextData.EngTitle = interestPoint.Data.Name.EngText;
        infoPopupData.PopupTextData.TextData.EngDescription = interestPoint.Data.InfoText.EngText;
        _collectPopupData.PopupTextData.TextData.EngDescription = interestPoint.Data.CollectText.EngText;

        _isActive = true;
    }

    protected override void Collect()
    {
        base.Collect();
        StartCoroutine(LoadInfoPopup(_collectPopupData.PopupTextData.Duration));
    }

    //Overrides for them to do nothing
    protected override void HandleLegStart(int leg)
    {

    }

    private IEnumerator LoadInfoPopup(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPopup_EC.RaiseEvent(infoPopupData);
    }
}
