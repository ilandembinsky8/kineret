using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ImageDataManager))]
public class GameDataManager : MonoBehaviour
{
    [SerializeField] private float loadingTime = 2f;

    #region variables
    private Dictionary<string, DestinationImageData> _destinationImageDataDiction;
    private Dictionary<string, Sprite> _iconImageDataMap;
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
    private void OnIconDataLoaded(string data)
    {
        IconData iconData = JsonUtility.FromJson<IconData>(data);

        _imageDataManager.LoadIcons(iconData, OnIconsFinLoading);
    }
    private void OnJsonDataLoaded(string data)
    {
        _currentGameData = JsonUtility.FromJson<GameData>(data);
        _imageDataManager.LoadImages(_currentGameData.DestinationDataList, OnImagesFinLoading);

        _jsonLoaded = true;
    }

    /// <summary>
    /// On data recieved from JSON containing names and sprites, sorts into comfortable Enum base
    /// </summary>
    private void OnIconsFinLoading(Dictionary<string, Sprite> iconImageDataMap)
    {
        _iconImageDataMap = iconImageDataMap;
        _iconsLoaded = true;
        _jsonManager.TryReadFromJson(OnJsonDataLoaded, "GameData.json");
    }
    private void OnImagesFinLoading(Dictionary<string, DestinationImageData> imageDataDictionaty)
    {
        _destinationImageDataDiction = imageDataDictionaty;
        _imagesLoaded = true;
        StartCoroutine(OnDataReadyToChangeScene());
    }
    #endregion

    private bool TryGetIconImageData(string iconName, out Sprite icon)
    {
        if (_iconImageDataMap != null &&
            _iconImageDataMap.TryGetValue(iconName, out icon)) { return true; }// Found image data for icon
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
            if(string.IsNullOrEmpty(_currentGameData.IntrestPointDataList[i].IconImageName))
            {
                Debug.LogError("Empty or null string when initializing a POI in GameDataManager");
                continue;
            }   
            
            TryGetIconImageData(_currentGameData.IntrestPointDataList[i].IconImageName, out Sprite iconImage);
            InterestPointData interestPointData = new InterestPointData { Data = _currentGameData.IntrestPointDataList[i], Icon = iconImage };
            LocationsManager.AddInterestPoint(i, interestPointData);
        }

        LocationsManager.InfoCollectable = _currentGameData.InfoCollectable;
        LocationsManager.BonusCollectables = _currentGameData.BonusCollectables;
        LocationsManager.InterestCollectable = _currentGameData.StaticInterestCollectable;
        LocationsManager.DestinationCollectables = _currentGameData.DestinationCollectables;
        LocationsManager.Challenges = _currentGameData.ChallengeDataList;
        LocationsManager.IconsMap = _iconImageDataMap;

        foreach (var item in LocationsManager.IconsMap)
        {
            Debug.Log($"String: {item.Key}, Value:{item.Value != null}");
        }

        LocationsManager.DestinationsCount = GameSettingsManager.GetInt("Game Settings", "DestinationGameCount", 3);
        LocationsManager.DestinationsSelectCount = GameSettingsManager.GetInt("Game Settings", "DestinationsSelectCount", 3);
        LocationsManager.SelectedDestinations = new int[LocationsManager.DestinationsSelectCount];
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