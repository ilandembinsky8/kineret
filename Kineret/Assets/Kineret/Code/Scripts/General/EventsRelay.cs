
using UnityEngine.Events;

public static class EventsRelay
{
    //Menus Events
    public static UnityAction<bool> OnEnableDestinationSelection { get; set; }

    //Game Events
    public static UnityAction OnGameStart { get; set; }
    public static UnityAction<bool> OnGamePause { get; set; }
    public static UnityAction OnGameOver { get; set; }

    public static UnityAction OnSummaryScreenReady { get; set; }

    public static UnityAction<int> OnScoreChange { get; set; }
    public static UnityAction<int> OnScoreGain { get; set; }

    public static UnityAction OnTextStarted { get; set; }
    //Destinations Events
    public static UnityAction<int> OnDestinationSelected { get; set; }
    public static UnityAction OnDestinationDeselected { get; set; }

    public static UnityAction<int> OnDestinationReached { get; set; }
    public static UnityAction<InfoScreenData> OnLoadInfoScreen { get; set; }
    public static UnityAction OnStartScoreCountdown { get; set; }

    public static UnityAction<int> OnLegStart { get; set; }
    //Player Events  
    public static UnityAction<ChallengeType,bool> OnWindEvent { get; set; }

    public static UnityAction OnShowDirection { get; set; }
    public static UnityAction<string> OnLoadDirectionPopup { get; set; }
}
