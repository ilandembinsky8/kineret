

using UnityEngine;

public class InfoPointHandler : CollectableHandler
{

    protected override void CheckNotifyRange(Vector3 delta)
    {
        //Overrides to not run base.CheckNotifyRange
    }

    public void Init(TextData infoData, InfoCollectableData collectableData)
    {
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        _collectPopupData.PopupTextData = collectableData.InfoPopup;
        InitPopup(ref _collectPopupData, collectableData.InfoPopup);
        _collectPopupData.PopupTextData.TextData.HebTitle = infoData.HebTitle;
        _collectPopupData.PopupTextData.TextData.HebDescription = infoData.HebDescription;
        _collectPopupData.PopupTextData.TextData.EngTitle = infoData.EngTitle;
        _collectPopupData.PopupTextData.TextData.EngDescription = infoData.EngDescription;
    }
}
