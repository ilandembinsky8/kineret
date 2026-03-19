

public class InfoPointHandler : InterestPointHandler
{
    public void Init(TextData infoData, InfoCollectableData collectableData)
    {
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        InitPopup(ref infoPopupData, collectableData.InfoPopup);      
        infoPopupData.PopupTextData.TextData.HebTitle = infoData.HebTitle;
        infoPopupData.PopupTextData.TextData.HebDescription = infoData.HebDescription;
    }
}
