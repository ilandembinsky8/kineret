using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SummaryScoreTab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Image selectedImage;

    private void OnDisable() { selectedImage.enabled = false; }

    public int GetIndexPosition() { return int.Parse(positionText.text); }
    public void ShowTabSelected()
    {
        selectedImage.enabled = true;
    }
    public void SetTabData(int position, string username, int score)
    {
        positionText.text = position.ToString();
        scoreText.text = score.ToString();
        usernameText.text = username;
    }

}