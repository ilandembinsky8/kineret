using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ImageDataManager))]
public class GameDataManager : MonoBehaviour
{
    [SerializeField] private float loadingTime = 2f;

    private Dictionary<string, DestinationImageData> _destinationImageDataDiction;
    private ImageDataManager _imageDataManager;
    private GameData _currentGameData;
    private JsonManager _jsonManager;
    private bool _jsonLoaded = false;
    private bool _imagesLoaded = false;
    private WaitForSeconds _loadingWait;

    private void Awake()
    {
        _jsonManager = new JsonManager();
        _loadingWait = new WaitForSeconds(loadingTime);
        _imageDataManager = GetComponent<ImageDataManager>();
    }
    private void Start() { _jsonManager.LoadFromJson(OnJsonDataLoaded); }

    public bool GetDestinationImageData(string destinationName, out DestinationImageData data)
    {
        if (_destinationImageDataDiction != null &&
            _destinationImageDataDiction.TryGetValue(destinationName, out data)) { return true; }// Found image data for destination

        data = default;
        return false; // No image found for destination
    }

    private void OnJsonDataLoaded(GameData data)
    {
        _currentGameData = data;
        _jsonLoaded = true;
        _imageDataManager.LoadImages(data.DestinationDataList, OnImagesFinLoading);
    }
    private void OnImagesFinLoading(Dictionary<string, DestinationImageData> imageDataDictionaty)
    {
        _imagesLoaded = true;
        StartCoroutine(OnDataReadyToChangeScene());
    }

    /// <summary>
    /// Launch the Menue Scene once the data is ready
    /// </summary>
    private void ChangeSceneOnDataLoaded()
    {
        if (!_jsonLoaded || !_imagesLoaded) { return; }

        LoadDataIntoLocationManager();
        SceneManager.LoadScene("Main Menu Scene");
    }

    private void LoadDataIntoLocationManager()
    {
        // destination data
        for (int i = 0; i < _currentGameData.DestinationDataList.Count; i++)
        {
            DestinationTextData destinationData = _currentGameData.DestinationDataList[i];

            GetDestinationImageData(destinationData.UIDestinationInfoText.EngTitle, out DestinationImageData destinationImageData);
            DestinationData fullDestinationData = new DestinationData
            {
                Data = destinationData,
                Background = destinationImageData.backgroundImage,
                Icon = destinationImageData.IconImage,
                Logo = destinationImageData.LogoImage,
                LogoScaleModifier = 1
            };

            LocationsManager.AddDestination(i, fullDestinationData);
        }

        // interest point data
        for (int i = 0; i < _currentGameData.IntrestPointDataList.Count; i++)
        {
            InterestPointData interestPointData = new InterestPointData { Data = _currentGameData.IntrestPointDataList[i] };
            // add icons later
            LocationsManager.AddInterestPoint(i, interestPointData);
        }

        LocationsManager.InfoCollectable = _currentGameData.InfoCollectable;
        LocationsManager.BonusCollectables = _currentGameData.BonusCollectables;
        LocationsManager.InterestCollectable = _currentGameData.StaticInterestCollectable;
        LocationsManager.DestinationCollectables = _currentGameData.DestinationCollectables;


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