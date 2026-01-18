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

    private void Awake()
    {
        if (data != null) LoadData(data);
    }

    public void LoadData(PopupData data)
    {
        if (data == null)
        {
            Destroy(gameObject);
            return;
        }
        
        if (data.Title != null && titleText != null) 
        {
            titleText.text = data.Title;
            titleText.fontStyle = FontStyles.Bold;
        } 
        if (data.Description != null && descriptionText != null) descriptionText.text = data.Description;
        if (data.IconSprite != null && iconImage != null)
        {
            iconImage.sprite = data.IconSprite;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(iconImage.sprite.texture.width, iconImage.sprite.texture.height);
        }
       

        StartCoroutine(Duration(data.Duration));
    }

    private IEnumerator Duration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

}
