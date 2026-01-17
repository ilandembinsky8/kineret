using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUpHandler : MonoBehaviour
{
    [SerializeField] private PopUpData data;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    private void Awake()
    {
        if (data != null) LoadData(data);
        
    }

    public void LoadData(PopUpData data)
    {
        if (data == null)
        {
            Destroy(gameObject);
            return;
        }
        
        if (data.TitleText != null && titleText != null) 
        {
            titleText.text = data.TitleText;
            titleText.fontStyle = FontStyles.Bold;
        } 
        if (data.DescriptionText != null && descriptionText != null) descriptionText.text = data.DescriptionText;
        if (data.IconSprite != null && iconImage != null) iconImage.sprite = data.IconSprite;

        StartCoroutine(Duration(data.Duration));
    }

    private IEnumerator Duration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

}
