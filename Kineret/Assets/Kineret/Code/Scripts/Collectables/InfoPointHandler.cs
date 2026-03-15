using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPointHandler : InterestPointHandler
{
    public void Init(DestinationInfoPointData infoData, InfoCollectableData collectableData)
    {
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData.Title = infoData.Title;
        infoPopupData.PopupTextData.Description = infoData.Description;
    }
}
