using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NarrationManager : MonoBehaviour
{
    private const string NARRATION_FOLDER = "Narration";
    private static NarrationManager _Instance;
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
        foreach (var destination in destinationDataList)
        {    
            yield return StartCoroutine(LoadDestinationNarration(destination.CodeName));      
        }

        onFinished?.Invoke();
    }

    private IEnumerator LoadDestinationNarration(string destinationName)
    {
        string fileName = $"{destinationName}_Narration.wav";
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
                _narrations.Add(destinationName, DownloadHandlerAudioClip.GetContent(www));
            }
        }
    }
}
