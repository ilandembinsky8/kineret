using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

//On Demand DDOL Signleton - DO NOT ADD TO A SCENE
public class NarrationManager : MonoBehaviour
{
    private const string NARRATION_FOLDER = "Narration";
    private const string FILE_CLOSER = "_Narration.wav";
    private const string INSTRUCTION_FILE_NAME = "Instruction";

    private static NarrationManager _Instance;

    private string _currentDestinationName;

    public static NarrationManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new GameObject().AddComponent<NarrationManager>();
                _Instance.name = _Instance.GetType().ToString();
                DontDestroyOnLoad(_Instance.gameObject);
            }
            return _Instance;
        }
    }

    private Dictionary<string, AudioClip> _narrations;
    public AudioClip InstrcutionNarration;


    private void Awake()
    {
        _narrations = new Dictionary<string, AudioClip>(16);
    }

    public AudioClip GetNarration(string destinationName)
    {
        return _narrations[destinationName];
    }

    public IEnumerator LoadNarration(List<DestinationTextData> destinationDataList,Action onFinished)
    {
        string fileName;
        foreach (var destination in destinationDataList)
        {
            _currentDestinationName = destination.CodeName;
            fileName = $"{_currentDestinationName}{FILE_CLOSER}";
            yield return StartCoroutine(LoadNarration(fileName, AddDestinationNarration));      
        }

        fileName = $"{INSTRUCTION_FILE_NAME}{FILE_CLOSER}";
        yield return StartCoroutine(LoadNarration(fileName, AddInstrcutionNarration));

        onFinished?.Invoke();
    }

    private void AddDestinationNarration(AudioClip clip)
    {
        _narrations.Add(_currentDestinationName, clip);
    }
    private void AddInstrcutionNarration(AudioClip clip)
    {
        InstrcutionNarration = clip;
    }

    private IEnumerator LoadNarration(string fileName,Action<AudioClip> OnFinishedLoad)
    {
       
        string fullPath = Path.Combine(Application.streamingAssetsPath, NARRATION_FOLDER, fileName);
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                OnFinishedLoad.Invoke(DownloadHandlerAudioClip.GetContent(www));
            }
        }
    }
}
