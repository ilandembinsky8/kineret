using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource _MusicSource;
    [SerializeField] private AudioClip[] _FlightMusic;
    [SerializeField] private AudioSource _NarrationSource;
    [SerializeField] private AudioSource _NotificationSource;
    [SerializeField] private AudioSource _OnCollectedSource;
    [SerializeField] private AudioClip[] _NarrationInteresetPoints;
    [SerializeField] private AudioClip _OpenUI;
    [SerializeField] private AudioClip _PointCollected;
    [SerializeField] private AudioClip _ArrivedDestination;
    [SerializeField] private AudioClip _CloudChallenge;
    [SerializeField] private AudioClip _BirdChallenge;
    [SerializeField] private AudioClip _KeepFlightHeight;

    #region Clips Volume Strength
    private float _maxMusicVolume = 0.2f;
    private float _minMusicVolume = 0.1f;
    private float _challengeVolume = 0.75f;
    private float _destinationVolume = 0.7f;
    private float _onCollectedVolume = 0.35f;
    private float _keepFlightHeightVolume = 0.7f;
    private float _arrivedDestinationVolume = 0.2f;
    private float _interestPointVolume = 0.7f;
    private float _instructionVolume = 0.15f;
    private float _openUIVolume = 0.01f;
    #endregion

    private int _interestPointToPlayBeforeGolanNarration = -1;
    private int _reachedGoalTracker = 0;

    private class IPNarrationClip
    {
        public AudioClip Clip;
        public float Volume;
    }

    private Queue<IPNarrationClip> _iPNarrationQueue = new Queue<IPNarrationClip>();
    private bool _isIPNarrationPlaying = false;
    private IPNarrationClip _pausedClip = null;
    private float _pausedClipTime = 0f;
    private List<IPNarrationClip> _savedQueue = null;
    private Coroutine _narrationCoroutine = null;

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
        SetLowerMusicVolume();
        _NotificationSource.loop = false;
        _NotificationSource.playOnAwake = false;
        _OnCollectedSource.loop = false;
        _OnCollectedSource.playOnAwake = false;
        _NarrationSource.loop = false;
        _NarrationSource.playOnAwake = false;
        _reachedGoalTracker = 0;
        _iPNarrationQueue.Clear();
        _isIPNarrationPlaying = false;
    }
    private void OnEnable()
    {
        EventsRelay.OnLoadDirectionPopup += TryPlayGolanNarration;
    }
    private void OnDisable()
    {
        EventsRelay.OnLoadDirectionPopup -= TryPlayGolanNarration;
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

    public void PlaySteadyFlightHeight(float delay) { StartCoroutine(PlaySteadyFlightHeightDelayed(delay)); }
    public void SetIncreasedMusicVolume() { _MusicSource.volume = _maxMusicVolume; }
    public void SetLowerMusicVolume() { _MusicSource.volume = _minMusicVolume; }
    public void PlayOpenUI() { PlayClipInSource(_OpenUI, _NotificationSource, _openUIVolume); }
    public void PlayPointCollected() { PlayClipInSource(_PointCollected, _OnCollectedSource, _onCollectedVolume); }
    public void PlayArrivedDestination() { PlayClipInSource(_ArrivedDestination, _OnCollectedSource, _arrivedDestinationVolume); }
    //public void PlayInterestPointNarration() { PlayClipInSource(, _NarrationSource, _interestPointVolume); }

    /// <summary>
    /// Saving the Interest point which Golan plays narration before said flight stretch
    /// </summary>
    /// <param name="playBeforeInterestPoint"></param>
    public void SetGolanNarrationBeforeXIP(int playBeforeInterestPoint) { _interestPointToPlayBeforeGolanNarration = playBeforeInterestPoint; }
    private void TryPlayGolanNarration(string direction)
    {
        if (_interestPointToPlayBeforeGolanNarration == -1) { return; }// Not selected Golan interest point

        _reachedGoalTracker++;

        if (_interestPointToPlayBeforeGolanNarration == _reachedGoalTracker)
        {
            PlayInterestPointNarration("IP-Golan");
            //reset after passing the interest point
            _interestPointToPlayBeforeGolanNarration = -1;
            _reachedGoalTracker = 0;
        }
    }

    public void PlayChallengeNarration(ChallengeType challengeType)
    {
        AudioClip challengeClip = null;
        switch (challengeType)
        {
            case ChallengeType.Clouds:
                challengeClip = _CloudChallenge;
                break;
            case ChallengeType.Birds:
                challengeClip = _BirdChallenge;
                break;
            case ChallengeType.SideWind:
                break;
            default:
                break;
        }

        StartCoroutine(PlayChallengeNarrationCoroutine(challengeClip));
    }

    private IEnumerator PlayChallengeNarrationCoroutine(AudioClip challengeClip)
    {
        PauseNarration();
        SetLowerMusicVolume();

        PlayClipInSource(challengeClip, _NotificationSource, _challengeVolume);
        yield return new WaitForSeconds(challengeClip.length);

        ResumeNarration();
    }
    public void PlayDestinationNarration(string destinationName)
    {
        StopNarration();
        SetLowerMusicVolume();
        AudioClip narration = NarrationManager.Instance.GetNarration(destinationName);
        PlayClipInSource(narration, _NarrationSource, _destinationVolume);
    }
    public void PlayInstructionNarration()
    {
        AudioClip narration = NarrationManager.Instance.InstrcutionNarration;
        PlayClipInSource(narration, _NarrationSource, _instructionVolume);
    }
    public void PlayInterestPointNarration(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("AudioManager: Interest point audio clip name is null or empty.");
            return;
        }

        AudioClip clip = FindAudioIPClipByName(clipName);
        if (clip != null)
        {
            PlayClipInSource(clip, _NarrationSource, _interestPointVolume);
            SetLowerMusicVolume();
            StartCoroutine(PlayPolygonShape(clipName, clip.length));
        }
        else
        {
            Debug.LogWarning($"AudioManager: Interest point narration clip '{clipName}' not found in _NarrationInteresetPoints array.");
        }
    }

    public void StopNarration()
    {
        _NarrationSource.Stop();
        _iPNarrationQueue.Clear();
        _isIPNarrationPlaying = false;
        _pausedClip = null;
        _savedQueue = null;

        if (_narrationCoroutine != null)
        {
            StopCoroutine(_narrationCoroutine);
            _narrationCoroutine = null;
        }
    }

    public void PauseNarration()
    {
        if (_isIPNarrationPlaying)
        {
            _pausedClip = new IPNarrationClip { Clip = _NarrationSource.clip, Volume = _NarrationSource.volume };
            _pausedClipTime = _NarrationSource.time;
            _NarrationSource.Stop();
            _isIPNarrationPlaying = false;

            _savedQueue = new List<IPNarrationClip>(_iPNarrationQueue);

            if (_narrationCoroutine != null)
            {
                StopCoroutine(_narrationCoroutine);
                _narrationCoroutine = null;
            }
        }
    }

    public void ResumeNarration()
    {
        if (_pausedClip != null)
        {
            _iPNarrationQueue.Clear();
            _iPNarrationQueue.Enqueue(_pausedClip);

            if (_savedQueue != null)
            {
                foreach (IPNarrationClip clip in _savedQueue)
                {
                    _iPNarrationQueue.Enqueue(clip);
                }
                _savedQueue = null;
            }

            _pausedClip = null;
            _narrationCoroutine = StartCoroutine(HandleNarrationQueue());
        }
    }

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
        SetLowerMusicVolume();
        _MusicSource.Play();
    }

    private void PlayClipInSource(AudioClip audioClip, AudioSource source, float volume)
    {
        if (source == null) { Debug.LogError("Source is null!"); return; }
        if (audioClip == null) { Debug.LogError("Audio clip is null!"); return; }

        SetLowerMusicVolume();

        if (source == _NarrationSource)
        {
            IPNarrationClip recievedAudioClip = new IPNarrationClip { Clip = audioClip, Volume = volume };
            _iPNarrationQueue.Enqueue(recievedAudioClip);

            if (!_isIPNarrationPlaying)
            {
                _narrationCoroutine = StartCoroutine(HandleNarrationQueue());
            }
        }
        else
        {
            source.clip = audioClip;
            source.volume = volume;
            source.Play();
        }
    }
    private AudioClip FindAudioIPClipByName(string clipName)
    {
        foreach (AudioClip clip in _NarrationInteresetPoints)
        {
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
        }
        return null;
    }

    private IEnumerator HandleNarrationQueue()
    {
        _isIPNarrationPlaying = true;

        while (_iPNarrationQueue.Count > 0)
        {
            IPNarrationClip iPNarrationClip = _iPNarrationQueue.Dequeue();

            _NarrationSource.clip = iPNarrationClip.Clip;
            _NarrationSource.volume = iPNarrationClip.Volume;

            float playClipTime = 0f;
            if (_pausedClipTime > 0f)
            {
                playClipTime = Mathf.Max(0f, _pausedClipTime - 1f);
                _pausedClipTime = 0f;
            }

            _NarrationSource.time = playClipTime;
            _NarrationSource.Play();

            yield return new WaitForSeconds(iPNarrationClip.Clip.length - playClipTime);
        }

        _isIPNarrationPlaying = false;
        _narrationCoroutine = null;
        SetIncreasedMusicVolume();
    }
    private IEnumerator WaitForTimeEnd(float waitLength, Action OnAudioFinished)
    {
        yield return new WaitForSeconds(waitLength);

        OnAudioFinished();
    }
    private IEnumerator PlayPolygonShape(string interestPointName, float clipTime)
    {
        SetLowerMusicVolume();
        MapShapesManager.Instance.Show(interestPointName);
        yield return new WaitForSeconds(clipTime);
        //MapShapesManager.Instance.Hide(interestPointName);
        SetIncreasedMusicVolume();
    }
    private IEnumerator PlaySteadyFlightHeightDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayClipInSource(_KeepFlightHeight, _NarrationSource, _keepFlightHeightVolume);
        SetLowerMusicVolume();
        yield return new WaitForSeconds(_KeepFlightHeight.length);
        SetIncreasedMusicVolume();
    }

}