using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ImageDataManager))]
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    public Dictionary<string, DestinationImageData> CurrentImageData { get { return _destinationImageDataDiction; } }
    public GameData CurrentGameData { get { return _currentGameData; } }

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        if (_jsonLoaded && _imagesLoaded)
        {
            SceneManager.LoadScene("Main Menu Scene");
        }
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