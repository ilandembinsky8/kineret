using System.Collections;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    [SerializeField] protected PopupData infoPopupData;

    private bool _hasInitPlayerPositionCollected = false;
    private Vector3 _initPlayerPosition;
    private string _audioClipName;

    public void Init(InterestPointData interestPoint, InfoCollectableData collectableData)
    {
        Init(collectableData.RangeData, collectableData.CollectionPopup, collectableData.NotificationPopup);
        _audioClipName = interestPoint.Data.AudioClipName;
        infoPopupData.PopupTextData = collectableData.InfoPopup;
        infoPopupData.IconSprite = interestPoint.Icon;

        infoPopupData.PopupTextData.TextData.HebTitle = interestPoint.Data.Name.HebText;
        infoPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.InfoText.HebText;
        _collectPopupData.PopupTextData.TextData.HebDescription = interestPoint.Data.CollectText.HebText;
        infoPopupData.PopupTextData.TextData.EngTitle = interestPoint.Data.Name.EngText;
        infoPopupData.PopupTextData.TextData.EngDescription = interestPoint.Data.InfoText.EngText;
        _collectPopupData.PopupTextData.TextData.EngDescription = interestPoint.Data.CollectText.EngText;

        _isActive = true;
        visuals.SetActive(false);
    }

    protected override void Collect()
    {
        //base.Collect();
        //StartCoroutine(LoadInfoPopup(_collectPopupData.PopupTextData.Duration));
        //AudioManager.Instance.PlayInterestPointNarration();
    }

    protected override void Notify(bool visualsOn)
    {
        base.Notify(visualsOn);
        if (!string.IsNullOrEmpty(_audioClipName))
        {
            AudioManager.Instance.PlayInterestPointNarration(_audioClipName);
        }
    }

    protected override void HandlePlayerMoved(Transform playerTransform)
    {
        base.HandlePlayerMoved(playerTransform);

        if(!_hasInitPlayerPositionCollected)
        {
            _initPlayerPosition = playerTransform.position;
            _hasInitPlayerPositionCollected = true;
        }

        if(gameObject.activeInHierarchy && _initPlayerPosition != null)
        {
            float heightOffset = playerTransform.position.y - _initPlayerPosition.y;
            float scaleFactor = 0.5f; //Ranges increase/decrease by 0.5 units of height offset
            SetRangesByHeightOffset(heightOffset, scaleFactor);
        }
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
