using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public enum IconType
{
    DestinationReached,
    InterestPoint,
    Challenge,
    Success,
    Forest,
    Bonus,
    Time,
    Info
}

[RequireComponent(typeof(ImageDataManager))]
public class GameDataManager : MonoBehaviour
{
    [SerializeField] private float loadingTime = 2f;

    #region variables
    private Dictionary<string, DestinationImageData> _destinationImageDataDiction;
    private static Dictionary<IconType, Sprite> _iconImageTypeDataDiction;
    private Dictionary<string, Sprite> _iconImageDataDiction;
    private ImageDataManager _imageDataManager;
    private WaitForSeconds _loadingWait;
    private GameData _currentGameData;
    private JsonManager _jsonManager;
    private bool _jsonLoaded = false;
    private bool _iconsLoaded = false;
    private bool _imagesLoaded = false;
    #endregion

    private void Awake()
    {
        _jsonManager = new JsonManager();
        _loadingWait = new WaitForSeconds(loadingTime);
        _imageDataManager = GetComponent<ImageDataManager>();
    }
    private void Start() { _jsonManager.TryReadFromJson(OnIconDataLoaded, "IconGameData.json"); }

    #region Callbacks for data loading
    void OnIconDataLoaded(string data)
    {
        IconData iconData = JsonUtility.FromJson<IconData>(data);

        _imageDataManager.LoadIcons(iconData, OnIconsFinLoading);
    }
    void OnJsonDataLoaded(string data)
    {
        _currentGameData = JsonUtility.FromJson<GameData>(data);
        _imageDataManager.LoadImages(_currentGameData.DestinationDataList, OnImagesFinLoading);

        _jsonLoaded = true;
    }

    /// <summary>
    /// On data recieved from JSON containing names and sprites, sorts into comfortable Enum base
    /// </summary>
    /// <param name="iconImageDataDictionaty"></param>
    void OnIconsFinLoading(Dictionary<string, Sprite> iconImageDataDictionaty)
    {
        _iconImageDataDiction = iconImageDataDictionaty;

        #region filling a usable Dictionary using IconType Enum
        _iconImageTypeDataDiction = new Dictionary<IconType, Sprite>(_iconImageDataDiction.Count);
        Dictionary<IconType, Sprite> iconEnumDictionary = new Dictionary<IconType, Sprite>();

        foreach (var item in iconImageDataDictionaty)
        {
            string iconName = item.Key;
            string[] iconNameFormatted = iconName.Split('-');// Expected format: "Icon-Category-Type"
            IconType iconType = default;

            switch (iconNameFormatted[2])//we are interested in the Type part
            {
                case "DestinationReached":
                    iconType = IconType.DestinationReached;
                    break;
                case "InterestPoint":
                    iconType = IconType.InterestPoint;
                    break;
                case "Challenge":
                    iconType = IconType.Challenge;
                    break;
                case "Success":
                    iconType = IconType.Success;
                    break;
                case "Forest":
                    iconType = IconType.Forest;
                    break;
                case "Bonus":
                    iconType = IconType.Bonus;
                    break;
                case "Time":
                    iconType = IconType.Time;
                    break;
                case "Info":
                    iconType = IconType.Info;
                    break;
                default:
                    Debug.LogError($"Icon name {iconName} does not match any known type, please varify Json name");
                    break;
            }

            _iconImageTypeDataDiction[iconType] = item.Value;
        }
        #endregion

        _iconsLoaded = true;
        _jsonManager.TryReadFromJson(OnJsonDataLoaded, "GameData.json");
    }
    void OnImagesFinLoading(Dictionary<string, DestinationImageData> imageDataDictionaty)
    {
        _destinationImageDataDiction = imageDataDictionaty;
        _imagesLoaded = true;
        StartCoroutine(OnDataReadyToChangeScene());
    }
    #endregion

    public static bool TryGetIconImageData(IconType iconType, out Sprite icon)
    {
        if (_iconImageTypeDataDiction != null &&
            _iconImageTypeDataDiction.TryGetValue(iconType, out icon)) { return true; }// Found image data for icon
        icon = default;
        return false; // No image found for icon
    }

    private bool TryGetIconImageData(string iconName, out Sprite icon)
    {
        if (_iconImageDataDiction != null &&
            _iconImageDataDiction.TryGetValue(iconName, out icon)) { return true; }// Found image data for icon
        icon = default;
        return false; // No image found for icon
    }
    private bool TryGetDestinationImageData(string destinationName, out DestinationImageData data)
    {
        if (_destinationImageDataDiction != null &&
            _destinationImageDataDiction.TryGetValue(destinationName, out data)) { return true; }// Found image data for destination

        data = default;
        return false; // No image found for destination
    }

    private void LoadDataIntoLocationManager()
    {
        // destination data
        for (int i = 0; i < _currentGameData.DestinationDataList.Count; i++)
        {
            DestinationTextData destinationTextData = _currentGameData.DestinationDataList[i];

            TryGetDestinationImageData(destinationTextData.UIDestinationInfoText.EngTitle, out DestinationImageData destinationImageData);
            TryGetIconImageData("Icon-Destination-Info", out Sprite iconImage);

            DestinationData destinationData = new DestinationData
            {
                Data = destinationTextData,
                Background = destinationImageData.backgroundImage,
                Icon = iconImage,
                Logo = destinationImageData.LogoImage,
                LogoScaleModifier = 1
            };

            LocationsManager.AddDestination(i, destinationData);
        }

        // interest point data
        for (int i = 0; i < _currentGameData.IntrestPointDataList.Count; i++)
        {
            TryGetIconImageData(_currentGameData.IntrestPointDataList[i].IconImageName, out Sprite iconImage);
            InterestPointData interestPointData = new InterestPointData { Data = _currentGameData.IntrestPointDataList[i], Icon = iconImage };
            LocationsManager.AddInterestPoint(i, interestPointData);
        }

        LocationsManager.InfoCollectable = _currentGameData.InfoCollectable;
        LocationsManager.BonusCollectables = _currentGameData.BonusCollectables;
        LocationsManager.InterestCollectable = _currentGameData.StaticInterestCollectable;
        LocationsManager.DestinationCollectables = _currentGameData.DestinationCollectables;
        LocationsManager.Challenges = _currentGameData.ChallengeDataList;

    }

    /// <summary>
    /// Launch the Menue Scene once the data is ready
    /// </summary>
    void ChangeSceneOnDataLoaded()
    {
        if (!_jsonLoaded || !_imagesLoaded || !_iconsLoaded) { Debug.LogError("Data wasn't loaded properly"); return; }

        LoadDataIntoLocationManager();
        SceneManager.LoadScene("Main Menu Scene");
    }

    /// <summary>
    /// Adds delay to scene change, can add animation or whatever
    /// </summary>
    /// <returns></returns>
    private IEnumerator OnDataReadyToChangeScene()
    {
        yield return _loadingWait;
        ChangeSceneOnDataLoaded();
    }

}