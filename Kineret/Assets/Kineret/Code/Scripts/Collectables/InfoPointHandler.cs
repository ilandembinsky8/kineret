using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPointHandler : InterestPointHandler
{
    public void Init(TextData infoData, InfoCollectableData collectableData)
    {
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        infoPopupData.PopupTextData.TextData.HebTitle = infoData.HebTitle;
        infoPopupData.PopupTextData.TextData.HebDescription = infoData.HebDescription;
    }
}
