
using UnityEngine;
using UnityEngine.Events;

public static class EventsRelay
{
    public static UnityAction<int> OnDestinationSelected { get; set; }
    public static UnityAction OnDestinationDeselected { get; set; }

    public static UnityAction<int> OnDestinationReached { get; set; }
    public static UnityAction<InfoScreenData> OnLoadInfoScreen { get; set; }

    public static UnityAction<bool> OnGamePause { get; set; }
    public static UnityAction OnGameOver { get; set; }
}
