using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SummaryScoreTab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI indexPositionText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI nameText;

    private Image _myImage;

    private void Awake() { _myImage = GetComponent<Image>(); }
    private void OnDisable() { _myImage.enabled = false; }

    public void ShowTabSelected()
    {
        _myImage.enabled = true;
    }
    public void SetTabData(int index, string name, int score)
    {
        indexPositionText.text = index.ToString();
        scoreText.text = score.ToString();
        nameText.text = name;
    }

}