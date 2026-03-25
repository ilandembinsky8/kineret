using System.Collections;
using UnityEngine;
using System;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource _MusicSource;
    [SerializeField] private AudioClip[] _FlightMusic;
    [SerializeField] private AudioSource _NotificationSource;
    [SerializeField] private AudioSource _OnCollectedSource;
    [SerializeField] private AudioClip _OpenUI;
    [SerializeField] private AudioClip _PointCollected;
    [SerializeField] private AudioClip _ArrivedDestination;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _MusicSource.loop = false;
        _MusicSource.playOnAwake = false;
        _NotificationSource.loop = false;
        _NotificationSource.playOnAwake = false;
        _OnCollectedSource.loop = false;
        _OnCollectedSource.playOnAwake = false;
    }

    #region for testing - can be removed after connecting to actual game
    //private void Start() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
    //private void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    //private void OnDestroy() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    //private void OnSceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
    //{
    //    if (arg0.buildIndex == 2)
    //    {
    //        PlayFlightMusic();
    //        Invoke("EndFlightMusic", 40f);
    //    }
    //}
    #endregion

    public void SetIncreasedMusicVolume() { _MusicSource.volume = 0.25f; }
    public void SetLowerMusicVolume() { _MusicSource.volume = 0.1f; }
    public void PlayOpenUI() { PlayNotificationSource(_OpenUI); }
    public void PlayPointCollected() { PlayOnCompletionSource(_PointCollected); }
    public void PlayArrivedDestination() { PlayOnCompletionSource(_ArrivedDestination); }

    /// <summary>
    /// Activates at the end, turns off looping and plays the out music clip after the current one is done.
    /// </summary>
    public void EndFlightMusic()
    {
        _MusicSource.loop = false;
        StartCoroutine(WaitForTimeEnd(_MusicSource.clip.length - _MusicSource.time, PlayEndLoopingFlightMusic));
    }
    /// <summary>
    /// Activates the Game Music and switches to looping after first clip is done.
    /// </summary>
    public void PlayFlightMusic()
    {
        PlayFlightMusicSource(0);
        StartCoroutine(WaitForTimeEnd(_MusicSource.clip.length, PlayLoopingFlightMusic));
    }
    private void PlayLoopingFlightMusic()
    {
        PlayFlightMusicSource(1);
        _MusicSource.loop = true;
    }
    private void PlayEndLoopingFlightMusic()
    {
        PlayFlightMusicSource(2);
    }

    private void PlayFlightMusicSource(int clipID)
    {
        if (_MusicSource == null) { Debug.LogError("Music source is null!"); return; }
        _MusicSource.clip = _FlightMusic[clipID];
        SetIncreasedMusicVolume();
        _MusicSource.Play();
    }
    private void PlayNotificationSource(AudioClip audioClip)
    {
        if (_NotificationSource == null) { Debug.LogError("Notification Audio source is null!"); return; }
        _NotificationSource.clip = audioClip;
        _NotificationSource.volume = 0.25f;
        _NotificationSource.Play();
    }
    private void PlayOnCompletionSource(AudioClip audioClip)
    {
        if (_OnCollectedSource == null) { Debug.LogError("Completion Audio source is null!"); return; }
        _OnCollectedSource.clip = audioClip;
        _OnCollectedSource.volume = 0.25f;
        _OnCollectedSource.Play();
    }

    private IEnumerator WaitForTimeEnd(float waitLength, Action OnAudioFinished)
    {
        yield return new WaitForSeconds(waitLength);

        OnAudioFinished();
    }

}