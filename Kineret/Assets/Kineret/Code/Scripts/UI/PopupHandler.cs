using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHandler : MonoBehaviour
{
    [SerializeField] private PopupData data;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    public void LoadData(PopupData data)
    {
       
        if (data.PopupTextData.TextData.HebTitle != null && titleText != null) 
        {
            titleText.text = data.PopupTextData.TextData.HebTitle;
            titleText.fontStyle = FontStyles.Bold;
        } 

        if (data.PopupTextData.TextData.HebDescription != null && descriptionText != null) descriptionText.text = data.PopupTextData.TextData.HebDescription;

        if (data.IconSprite != null && iconImage != null)
        {
            iconImage.sprite = data.IconSprite;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(iconImage.sprite.texture.width, iconImage.sprite.texture.height);
        }
       
        StartCoroutine(Duration(data.PopupTextData.Duration));
    }

    public void ScaleIconeSize(float multiplier)
    {
        ((RectTransform)iconImage.transform).sizeDelta = new Vector2(iconImage.sprite.texture.width * multiplier, iconImage.sprite.texture.height * multiplier);
    }

    private IEnumerator Duration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
    public void ChangeDataAndReplayText(PopupData data)
    {
        LoadData(data);

        PopupTweenHandler tweenHandler = GetComponent<PopupTweenHandler>();
        if (tweenHandler != null)
        {
            tweenHandler.ReplayTextOnly();
        }
    }

}
