
using UnityEngine;

public class MainMenuDestinationLoader : MonoBehaviour
{
    [SerializeField] private RectTransform parent;
    [SerializeField] private DestinationButtonHandler destinationButtonPrefab;

    [SerializeField] private bool IsTesting;
    [SerializeField] private DestinationSO AgmonHula;
    [SerializeField] private DestinationSO Shamir;
    [SerializeField] private DestinationSO Gilboa;
    [SerializeField] private DestinationSO Agre;
    [SerializeField] private DestinationSO Golan;
    [SerializeField] private DestinationSO BioCastle;
    [SerializeField] private DestinationSO Salmon;
    [SerializeField] private DestinationSO Eshkol;
    [SerializeField] private DestinationSO Ginosar;
    [SerializeField] private DestinationSO Tzemah;
    [SerializeField] private DestinationSO Afimilk;

    [SerializeField] private InterestPointSO Firewave;
    [SerializeField] private InterestPointSO Seymour;
    [SerializeField] private InterestPointSO Shvitz;
    [SerializeField] private InterestPointSO Tzipori;

    void Awake()
    {

        if (IsTesting)
        {
            LocationsManager.AddDestination(9, AgmonHula);
            //LocationsManager.AddDestination(1, Shamir);
            LocationsManager.AddDestination(2, Gilboa);
            LocationsManager.AddDestination(3, Agre);
            LocationsManager.AddDestination(4, Golan);
            LocationsManager.AddDestination(5, BioCastle);
            LocationsManager.AddDestination(6, Salmon);
            LocationsManager.AddDestination(7, Eshkol);
            LocationsManager.AddDestination(8, Ginosar);
            //LocationsManager.AddDestination(9, Tzemah);
            LocationsManager.AddDestination(10, Afimilk);
            LocationsManager.AddInterestPoint(0, Firewave);
            LocationsManager.AddInterestPoint(1, Seymour);
            LocationsManager.AddInterestPoint(2, Shvitz);
            LocationsManager.AddInterestPoint(3, Tzipori);
        }

        for (int i = 0; i < LocationsManager.DestinationsCount; i++)
        {
            DestinationButtonHandler handler = Instantiate(destinationButtonPrefab, parent);
            handler.LoadDestination(i);
        }       
    }
}
