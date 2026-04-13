using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SummaryScoreHandler : MonoBehaviour
{
    [SerializeField] private SummaryScoreTab summaryScoreTabPrefab;
    [SerializeField] private GameObject playerScoreContainer;
    [SerializeField] private int maxScoreTabs = 4; //we can do 5 but XD seems to have 4

    private List<SummaryScoreTab> _summaryScoreTabs = new List<SummaryScoreTab>();

    public void ReorderScoreByIndex()
    {
        _summaryScoreTabs = _summaryScoreTabs.OrderBy(tab => tab.GetIndexPosition()).ToList();
        for (int i = 0; i < _summaryScoreTabs.Count; i++)
            _summaryScoreTabs[i].transform.SetSiblingIndex(i);
    }

    public void CreateNewScoreTab(int index, string name, int score)
    {
        SummaryScoreTab newSummaryScoreTab = BaseTabCreation(index, name, score);
    }
    public void CreateNewScoreTab(int index, string name, int score, bool isSelected)
    {
        SummaryScoreTab newSummaryScoreTab = BaseTabCreation(index, name, score);

        if (isSelected)
            newSummaryScoreTab.ShowTabSelected();
    }

    private SummaryScoreTab BaseTabCreation(int index, string name, int score)
    {
        SummaryScoreTab newSummaryScoreTab = Instantiate(summaryScoreTabPrefab, playerScoreContainer.transform);
        newSummaryScoreTab.SetTabData(index, name, score);
        _summaryScoreTabs.Add(newSummaryScoreTab);

        return newSummaryScoreTab;
    }

}