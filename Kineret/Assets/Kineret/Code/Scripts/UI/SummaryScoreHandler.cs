using UnityEngine;

public class SummaryScoreHandler : MonoBehaviour
{
    [SerializeField] private SummaryScoreTab summaryScoreTabPrefab;
    [SerializeField] private GameObject playerScoreContainer;
    [SerializeField] private int maxScoreTabs = 4; //we can do 5 but XD seems to have 4

    public void CreateNewScoreTab(int index, string name, int score)
    {
        SummaryScoreTab newSummaryScoreTab = Instantiate(summaryScoreTabPrefab, playerScoreContainer.transform);
        newSummaryScoreTab.SetTabData(index, name, score);
    }
    public void CreateNewScoreTab(int index, string name, int score, bool isSelected)
    {
        SummaryScoreTab newSummaryScoreTab = Instantiate(summaryScoreTabPrefab, playerScoreContainer.transform);
        newSummaryScoreTab.SetTabData(index, name, score);

        if (isSelected)
            newSummaryScoreTab.ShowTabSelected();
    }

}