using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class JsonManager
{
    private GameData Data;

    public bool LoadFromJson(Action<GameData> onFinished)
    {
        string debugState = $"Loading JSON... Path: {Application.streamingAssetsPath}";
        string path = Path.Combine(Application.streamingAssetsPath, "GameData.json");

        if (!File.Exists(path))
        {
            debugState = $"JsonManager: File not found at path: {path}";
            Debug.LogError(debugState);
            return false;
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            debugState = $"JsonManager: JSON is empty.";
            Debug.LogError(debugState);
            return false;
        }

        Data = JsonUtility.FromJson<GameData>(json);

        debugState = $"JsonManager: JSON loaded successfully.";
        onFinished?.Invoke(Data);
        return true;
    }

}

/////////////////////////////////////////////////////////////////////////////////////
///////////////////////         V Structs V          ////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////

[Serializable]
public struct GameData
{
    public List<DestinationTextData> DestinationDataList;

    public List<InfoCollectableData> StaticCollectableDataList;

    public List<BonusCollectableData> BonusDataList;

    public List<ChallengePointData> ChallengeDataList;
}

#region Destination Data

/// <summary>
/// Main data structure of a destination
/// </summary>
public struct DestinationData
{
    public DestinationTextData Data;
    public Sprite Background;
    public Sprite Logo;
    public Sprite Icon;
    public float LogoScaleModifier;
}

/// <summary>
/// Text data and positions of a destination
/// </summary>
[Serializable]
public struct DestinationTextData
{
    /// <summary>
    /// Represents the UI Map destination name and description.
    /// </summary>
    public InfoPointData UIDestinationInfoText;

    public Vector3 UiPosition;
    public Vector3 WorldPosition;
    public Vector3 WorldRotation;

    /// <summary>
    /// represents the info screen once destination was reached.
    /// </summary>
    public InfoPointData DestinationInfoScreenText;

    public InfoPointData FirstInfoPoint;
    public InfoPointData SecondInfoPoint;
    public InfoPointData ThirdInfoPoint;
}

#endregion

/// <summary>
/// This data represents the range/score and collection popup texts of a destination, once reached.
/// </summary>
[Serializable]
public struct DestinationCollectableData
{
    public CollectableData RangeData;

    public PopupTextData CollectionPopup;
}

#region Collectable Data

/// <summary>
/// Represents the data of Points of intereset and actual Hayadata (DestinationCollectableData)
/// </summary>
[Serializable]
public struct InfoCollectableData
{
    public CollectableData RangeData;

    public PopupTextData NotificationPopup;
    public PopupTextData CollectionPopup;
    public PopupTextData InfoPopup;
}

[Serializable]
public struct InterestPointTextData
{
    public string Name;
    public Vector3 WorldPosition;
    public string InfoText;
    public string CollectText;
}

[Serializable]
public struct BonusCollectableData
{
    public CollectableData CollectableData;

    public PopupTextData NotificationPopup;
    public PopupTextData CollectionPopup;
}

[Serializable]
public struct ChallengePointData
{
    //public float timeLimit; //gonna sit in .ini
    public CollectableData CollectableData;

    public PopupTextData NotificationPopup;
    public PopupTextData CompletedPopup;
}
#endregion

/// <summary>
/// Mirrors CollectableData fields
/// </summary>
[Serializable]
public struct CollectableData
{
    public float NotificationRange;
    public float CollectionRange;
    public float TimeForMaxScore;
    public int MaxScore;
}

/// <summary>
/// Mirrors PopupData fields
/// </summary>
[Serializable]
public struct PopupTextData
{
    public int Type;
    public InfoPointData TextData;
    public float Duration;
    public float Delay;
}

/// <summary>
/// This data holds 2 sets of title and description. one for Hebrew and one English.
/// </summary>
[Serializable]
public struct InfoPointData
{
    public string HebTitle;
    public string HebDescription;
    public string EngTitle;
    public string EngDescription;
}
