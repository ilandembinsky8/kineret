using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class JsonManager : MonoBehaviour
{
    public GameData Data;

    [SerializeField] CollectableData BonusPointRange;
    [SerializeField] CollectableData DestinationRange;
    [SerializeField] CollectableData InterestPointRange;

    private bool _dataIsLoaded;

    private void Awake() { _dataIsLoaded = LoadFromJson(); }

    [ContextMenu("Load From JSON")]
    public bool LoadFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "GameData.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning($"{nameof(JsonManager)}: File not found at path: {path}");
            return false;
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning($"{nameof(JsonManager)}: JSON is empty.");
            return false;
        }

        Data = JsonUtility.FromJson<GameData>(json);

        #region validation checks
        if (Data == null)
        {
            Debug.LogWarning($"{nameof(JsonManager)}: JSON is missing/empty.");
            return false;
        }

        if (Data.DestinationsPoolData == null)
        {
            Debug.LogWarning("DestinationsPoolData missing.");
            return false;
        }

        if (Data.ChallengesPoolData == null)
        {
            Debug.LogWarning("ChallengesPoolData missing.");
            return false;
        }

        if (Data.BonusPoolData == null)
        {
            Debug.LogWarning("BonusPoolData missing.");
            return false;
        }

        if (Data.StaticInterestPoolData == null)
        {
            Debug.LogWarning("StaticInterestPoolData missing.");
            return false;
        }

        /*foreach (var destination in Data.DestinationsPoolData)
        {
            if (destination.DidYouKnowPoints == null || destination.DidYouKnowPoints.Count != 3)
            {
                Debug.LogWarning("Destination doesn't contain exactly 3 DidYouKnowPoints.");
                return false;
            }
        }*/
        #endregion

        Debug.Log($"{nameof(JsonManager)}: JSON loaded successfully.");
        return true;
    }

}

[Serializable]
public class GameData
{
    //Tomer: I believe not using a list and having a field for each location would:
    //A. Remove the need of the ID in the json, as now I can match them to the enum by field.
    //B. Make it obvious which data goes where as that would be the name of the field and not a random spot in the collection
    //For example the field would be named TzemahData, can't imagine someone not realizing what data to put there.
    public List<DestinationData> DestinationsPoolData;
    public List<ChallengePoints> ChallengesPoolData;
    public List<CollectablePoints> BonusPoolData;
    public List<StaticInterestPoints> StaticInterestPoolData;
}

[Serializable]
public struct DestinationData
{
    public int Id;
    public string Name;
    public string Description;
    public Vector3 UiPosition;
    public Vector3 WorldPosition;

    public string InfoScreenSubtitle;
    public string InfoScreenText;
    
    public DestinationInfoPointData FirstInfoPoint;
    public DestinationInfoPointData SecondInfoPoint;
    public DestinationInfoPointData ThirdInfoPoint;
}
[Serializable]
public struct DestinationInfoPointData
{
    public string Title;
    public string Description;
}
[Serializable]
public struct InterestPointData
{
    public string Name;
    public Vector3 WorldPosition;
    public string Text;
}

[Serializable]
public class CollectablePoints
{
    public RangeData RangeData;

    public PopupText NotificationPopup;
    public PopupText CollectionPopup;
}

[Serializable]
public class StaticInterestPoints
{
    public Vector3 WorldPosition;

    public RangeData RangeData;

    public PopupText NotificationPopup;
    public PopupText CollectionPopup;
    public PopupText CollectedPopup;
}

[Serializable]
public class ChallengePoints
{
    //public float timeLimit; //gonna sit in .ini
    public RangeData RangeData;

    public PopupText NotificationPopup;
    public PopupText CompletedPopup;
}

/// <summary>
/// Mirrors CollectableData fields
/// </summary>
[Serializable]
public struct RangeData
{
    public float NotificationRange;
    public float CollectionRange;
    public int Score;
}

/// <summary>
/// Mirrors PopupData fields
/// </summary>
[Serializable]
public struct PopupText
{
    public string Title;
    public string Description;
    public float Duration;
    public float Delay;
}