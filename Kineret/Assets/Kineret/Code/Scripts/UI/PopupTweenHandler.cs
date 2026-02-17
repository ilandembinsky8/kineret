using DG.Tweening;
using System.Collections;
using UnityEngine;

public class PopupTweenHandler : MonoBehaviour
{

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform icon;
    [SerializeField] private RectTransform mainText;
    [SerializeField] private RectTransform subText;

    [SerializeField] private float backgroundTweenDuration;
    [SerializeField] private float contentDelay;

    private Vector2 _originalBGSizeDelta;

    private void Awake()
    {
        _originalBGSizeDelta = background.sizeDelta;
        icon.gameObject.SetActive(false);
        mainText.gameObject.SetActive(false);

        if(subText != null) subText.gameObject.SetActive(false);

        background.sizeDelta = new Vector2 (background.sizeDelta.x, 0);
        StartCoroutine(EnterAnimation());
    }

    private IEnumerator EnterAnimation()
    {
        background.DOSizeDelta(_originalBGSizeDelta, backgroundTweenDuration).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(contentDelay);
        icon.gameObject.SetActive(true);
        mainText.gameObject.SetActive(true);
    }


}
