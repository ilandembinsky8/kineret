
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

    void Awake()
    {

        if (IsTesting)
        {
            LocationsManager.AddDestination(Destination.AgmonHula, AgmonHula);
            LocationsManager.AddDestination(Destination.Shamir, Shamir);
            LocationsManager.AddDestination(Destination.Gilboa, Gilboa);
            LocationsManager.AddDestination(Destination.Agre, Agre);
            LocationsManager.AddDestination(Destination.Golan, Golan);
            LocationsManager.AddDestination(Destination.BioCastle, BioCastle);
            LocationsManager.AddDestination(Destination.Salmon, Salmon);
            LocationsManager.AddDestination(Destination.Eshkol, Eshkol);
            LocationsManager.AddDestination(Destination.Ginosar, Ginosar);
            LocationsManager.AddDestination(Destination.Tzemah, Tzemah);
            LocationsManager.AddDestination(Destination.Afimilk, Afimilk);
        }

        for (int i = 0; i < LocationsManager.DestinationsCount; i++)
        {
            DestinationButtonHandler handler = Instantiate(destinationButtonPrefab, parent);
            handler.LoadDestination((Destination)i);
        }       
    }
}
