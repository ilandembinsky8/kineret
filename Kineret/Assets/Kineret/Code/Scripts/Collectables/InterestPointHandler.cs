using System.Collections;
using UnityEngine;

public class InterestPointHandler : CollectableHandler
{
    //[SerializeField] protected GameObject ForestVisuals;
    //[SerializeField] protected GameObject MountainVisuals;
    [SerializeField] protected PopupData infoPopupData;
    [SerializeField] private System.Collections.Generic.List<SpriteRenderer> InterestPointSpriteCollection;

    Color _forestColor = new Color32(84, 254, 81, 255);
    Color _mountainColor = new Color32(81, 178, 248, 255);
    private bool _hasInitPlayerPositionCollected = false;
    private Vector3 _initPlayerPosition;
    private string _audioClipName;
    private string _myMapIconName;

    public void Init(InterestPointData interestPoint, InfoCollectableData collectableData)
    {
        _audioClipName = interestPoint.Data.AudioClipName;
        _myMapIconName = interestPoint.Data.IconImageName;

        if (_myMapIconName != null)
        {
            if (_myMapIconName == "Icon-Route-Forest")
                SetInterestPointSpriteColor(_forestColor);
            else
                SetInterestPointSpriteColor(_mountainColor);
        }

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
        visuals.SetActive(false);
    }

    protected override void Collect()
    {
        //base.Collect();
        //StartCoroutine(LoadInfoPopup(_collectPopupData.PopupTextData.Duration));
        //AudioManager.Instance.PlayInterestPointNarration();
    }

    protected override void UpdateVisual(string spriteStreamingName)
    {
        SpriteRenderer renderer = visuals.GetComponent<SpriteRenderer>();
        Sprite sprite;
        if (!LocationsManager.TryGetIconImageData(_myMapIconName, out sprite))
        {
            Debug.LogError($"Failed to pull image data for interest point icon: {_myMapIconName}");
        }
        renderer.sprite = sprite;
    }

    protected override void Notify(bool visualsOn)
    {
        visuals.SetActive(!visualsOn);//this is marked not as it's alawys sent as false.

        if (GameManager.IsPlayerCloseToAChallenge) return;

        base.Notify(visualsOn);
        if (!string.IsNullOrEmpty(_audioClipName))
        {
            AudioManager.Instance.PlayInterestPointNarration(_audioClipName);
            //MapShapesManager.Instance.Show(_audioClipName);

            //if (_audioClipName == "IP-Meron")
            //{
            //    if (MountainVisuals == null)
            //        Debug.LogError("MountainVisuals is not assigned in InterestPointHandler.");
            //    else
            //        MountainVisuals.SetActive(true);
            //}
            //else
            //{
            //    if (ForestVisuals == null)
            //        Debug.LogError("ForestVisuals is not assigned in InterestPointHandler.");
            //    else
            //        ForestVisuals.SetActive(true);
            //}
        }
    }

    protected override void HandlePlayerMoved(Transform playerTransform)
    {
        base.HandlePlayerMoved(playerTransform);

        if (!_hasInitPlayerPositionCollected)
        {
            _initPlayerPosition = playerTransform.position;
            _hasInitPlayerPositionCollected = true;
        }

        if (gameObject.activeInHierarchy && _initPlayerPosition != null)
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

    private void SetInterestPointSpriteColor(Color newColor)
    {
        foreach (SpriteRenderer spriteRenderer in InterestPointSpriteCollection)
            if (spriteRenderer != null)
                spriteRenderer.color = newColor;
    }

    private IEnumerator LoadInfoPopup(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPopup_EC.RaiseEvent(infoPopupData);
    }
}
