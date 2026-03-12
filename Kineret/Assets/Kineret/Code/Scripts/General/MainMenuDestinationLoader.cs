
using UnityEngine;

public class MainMenuDestinationLoader : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private DestinationButtonHandler destinationButtonPrefab;
    void Awake()
    {
        for (int i = 0; i < LocationsManager.DestinationsCount; i++)
        {
            DestinationButtonHandler handler = Instantiate(destinationButtonPrefab, parent);
            handler.LoadDestination((Destination)i);
        }       
    }
}
