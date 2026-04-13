using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SummaryScoreHandler : MonoBehaviour
{
    [SerializeField] private SummaryScoreTab summaryScoreTabPrefab;
    [SerializeField] private GameObject playerScoreContainer;
    //[SerializeField] private int maxScoreTabs = 4;

    //private List<SummaryScoreTab> _summaryScoreTabs;

    //private void Awake() { _summaryScoreTabs = new List<SummaryScoreTab>(maxScoreTabs); }

   /* public void ReorderScoreByIndex()
    {
        _summaryScoreTabs = _summaryScoreTabs.OrderBy(tab => tab.GetIndexPosition()).ToList();
        for (int i = 0; i < _summaryScoreTabs.Count; i++)
            _summaryScoreTabs[i].transform.SetSiblingIndex(i);
    }*/

    public void CreateNewScoreTab(int position, string username, int score, bool isSelected = false)
    {
        SummaryScoreTab newSummaryScoreTab = BaseTabCreation(position, username, score);

        if (isSelected)
            newSummaryScoreTab.ShowTabSelected();
    }

    private SummaryScoreTab BaseTabCreation(int position, string username, int score)
    {
        SummaryScoreTab newSummaryScoreTab = Instantiate(summaryScoreTabPrefab, playerScoreContainer.transform);
        newSummaryScoreTab.SetTabData(position, username, score);
        //_summaryScoreTabs.Add(newSummaryScoreTab);

        return newSummaryScoreTab;
    }

}