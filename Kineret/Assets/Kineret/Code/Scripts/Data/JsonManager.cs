using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class JsonManager : MonoBehaviour
{
    public GameData Data;

    #region Serialized fields
    [SerializeField] CollectableData BonusPointRange;
    [SerializeField] CollectableData DestinationRange;
    [SerializeField] CollectableData InterestPointRange;

    #region Destinations
    [Header("Destinations")]
    [SerializeField] private DestinationSO Afimilk;
    [SerializeField] private DestinationSO AgmonHula;
    [SerializeField] private DestinationSO Agre;
    [SerializeField] private DestinationSO BioCastle;
    [SerializeField] private DestinationSO Eshkol;
    [SerializeField] private DestinationSO Gilboa;
    [SerializeField] private DestinationSO Ginosar;
    [SerializeField] private DestinationSO Golan;
    [SerializeField] private DestinationSO Salmon;
    [SerializeField] private DestinationSO Shamir;
    [SerializeField] private DestinationSO Tzemah;
    #endregion

    [Header("Interest Points")]
    [SerializeField] private InterestPointSO Biriya;
    [SerializeField] private InterestPointSO Keshet;
    [SerializeField] private InterestPointSO SwitzForest;
    [SerializeField] private InterestPointSO Tzipori;
    #endregion

    private bool _dataIsLoaded;

    private void Awake() { _dataIsLoaded = LoadFromJson(); }
    private void Start()
    {
        if (_dataIsLoaded)
        {
            //Afimilk.DestinationData = Data.Afimilk;
            //AgmonHula.DestinationData = Data.AgmonHula;
            //Agre.DestinationData = Data.Agre;
            //BioCastle.DestinationData = Data.BioCastle;
            //Eshkol.DestinationData = Data.Eshkol;
            //Gilboa.DestinationData = Data.Gilboa;
            //Ginosar.DestinationData = Data.Ginosar;
            //Golan.DestinationData = Data.Golan;
            //Salmon.DestinationData = Data.Salmon;
            Shamir.DestinationData = Data.Shamir;
            Tzemah.DestinationData = Data.Tzemah;
        }
    }

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

        Debug.Log($"{nameof(JsonManager)}: JSON loaded successfully.");
        return true;
    }

}

/////////////////////////////////////////////////////////////////////////////////////
///////////////////////         V Structs V          ////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////

[Serializable]
public struct GameData
{
    public DestinationData Afimilk;
    public DestinationData AgmonHula;
    public DestinationData Agre;
    public DestinationData BioCastle;
    public DestinationData Eshkol;
    public DestinationData Gilboa;
    public DestinationData Ginosar;
    public DestinationData Golan;
    public DestinationData Salmon;
    public DestinationData Shamir;
    public DestinationData Tzemah;

    public InfoCollectableData Biriya;
    public InfoCollectableData Keshet;
    public InfoCollectableData SwitzForest;
    public InfoCollectableData Tzipori;

    public InfoCollectableData HayadataCollecableData;

    public BonusCollectableData CarbonBonusData;
    public BonusCollectableData EnergyBonusData;
    public BonusCollectableData WaterBonusData;
    public BonusCollectableData SoilBonusData;

    public ChallengePointData FrontWindChallengeData;
    public ChallengePointData SideWindChallengeData;
    public ChallengePointData BirdChallengeData;
}

#region Destination Data
/// <summary>
/// Main data structure of a destination
/// </summary>
[Serializable]
public struct DestinationData
{
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

/// <summary>
/// This data represents the text of one of the 3 info points in each destination.
/// </summary>
[Serializable]
public struct DestinationInfoPointData
{
    public string Title;
    public string Description;
}

/// <summary>
/// This data represents the range/score and collection popup texts of a destination, once reached.
/// </summary>
[Serializable]
public struct DestinationCollectableData
{
    public RangeCollectableData RangeData;

    public PopupTextData CollectionPopup;
}
#endregion

#region Collectable Data
/// <summary>
/// Represents the data of Points of intereset and actual Hayadata (DestinationCollectableData)
/// </summary>
[Serializable]
public struct InfoCollectableData
{
    public RangeCollectableData RangeData;

    public PopupTextData NotificationPopup;
    public PopupTextData CollectionPopup;
    public PopupTextData InfoPopup;
}

[Serializable]
public struct InterestPointData
{
    public string Name;
    public Vector3 WorldPosition;
    public string InfoText;
    public string CollectText;
}

[Serializable]
public struct BonusCollectableData
{
    public RangeCollectableData RangeData;

    public PopupTextData NotificationPopup;
    public PopupTextData CollectionPopup;
}

[Serializable]
public struct ChallengePointData
{
    //public float timeLimit; //gonna sit in .ini
    public RangeCollectableData RangeData;

    public PopupTextData NotificationPopup;
    public PopupTextData CompletedPopup;
}
#endregion

/// <summary>
/// Mirrors CollectableData fields
/// </summary>
[Serializable]
public struct RangeCollectableData
{
    public float NotificationRange;
    public float CollectionRange;
    public int Score;
}

/// <summary>
/// Mirrors PopupData fields
/// </summary>
[Serializable]
public struct PopupTextData
{
    public int Type;
    public string Title;
    public string Description;
    public float Duration;
    public float Delay;
}