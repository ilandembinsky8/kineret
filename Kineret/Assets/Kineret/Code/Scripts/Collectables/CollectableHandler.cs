using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollectableHandler : MonoBehaviour
{
    [SerializeField] protected PopupDataEventChannel LoadPopup_EC;
    [SerializeField] protected TransformEventChannel playerMoved_EC;

    [SerializeField] protected CollectableData _collectableData;
    [SerializeField] protected PopupData _notificationPopupData;
    [SerializeField] protected PopupData _collectPopupData;

    [SerializeField] protected GameObject visuals;

    protected bool _hasNotified;
    protected bool _wasCollected;
    protected bool _isCollectable;
    protected bool _isActive;
    protected int _score;

    protected Color _notifyColor = Color.red;

    private InputActions _actions;
    private Coroutine _scoreCoroutine;
    public int Leg { get; set; }

    public int MaxScore 
    {
        get => _collectableData.MaxScore;
    }

    private void Awake()
    {
        _actions = new InputActions();
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.Collect.performed += HandleCollectInput;
        playerMoved_EC.OnEventRaised += HandlePlayerMoved;
        EventsRelay.OnLegStart += HandleLegStart;
    }

    protected void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Collect.performed -= HandleCollectInput;
        playerMoved_EC.OnEventRaised -= HandlePlayerMoved;
        EventsRelay.OnLegStart -= HandleLegStart;
    }
    private void Update()
    {
        if(Input.GetButtonDown(JoystickManager.JoystickControls.Trigger)) { HandleCollectInput(); }
    }

    private void OnDrawGizmos()
    {
        if (_collectableData.NotificationRange > 0)
        {
            Handles.color = _notifyColor;
            Handles.DrawWireDisc(transform.position, new Vector3(0f, 1f, 0f), _collectableData.NotificationRange);
        }
        if (_collectableData.CollectionRange > 0)
        {
            Handles.color = Color.green;
            Handles.DrawWireDisc(transform.position, new Vector3(0f, 1f, 0f), _collectableData.CollectionRange);
        }           
    }

    public void Init(CollectableData collectableData, PopupTextData collectPopupData, PopupTextData notificationPopupData = new PopupTextData())
    {
        _collectableData = collectableData;
        _score = _collectableData.MaxScore;
        InitPopup(ref _notificationPopupData, notificationPopupData);
        InitPopup(ref _collectPopupData, collectPopupData);
        UpdateVisual(_notificationPopupData.IconSprite);
    }

    protected virtual void UpdateVisual(Sprite sprite)
    {
        SpriteRenderer renderer = visuals.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
    }

    protected virtual void HandleLegStart(int leg)
    {
        _isActive = leg == Leg;
        visuals.SetActive(leg == Leg);
    }
    protected void InitPopup(ref PopupData popupData, PopupTextData popupTextData)
    {
        popupData.PopupTextData = popupTextData;
        if (!String.IsNullOrEmpty(popupTextData.PopupIconName))
        {
            LocationsManager.TryGetIconImageData(popupTextData.PopupIconName, out popupData.IconSprite);
        }
        else
        {
            Debug.Log($"Null icon name in a popup text data. Class:{GetType().Name},Title:{popupTextData.TextData.HebTitle}");
        }
    }

    protected virtual void HandlePlayerMoved(Transform playerTransform)
    {
        if (!_isActive) return;

        Vector3 playerPositionXZ = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
        Vector3 collectablePositionXZ = new Vector3(transform.position.x, 0f, transform.position.z);

        Vector3 delta = playerPositionXZ - collectablePositionXZ;

        CheckNotifyRange(delta);
        CheckCollectRange(delta);          
    }
    protected virtual void CheckNotifyRange(Vector3 delta)
    {
        if (_hasNotified) return;
        if (delta.sqrMagnitude <= _collectableData.NotificationRange * _collectableData.NotificationRange)
        {
            Notify();
        }
    }
    protected virtual void CheckCollectRange(Vector3 delta)
    {
        if (_wasCollected) return;
        _isCollectable = false;
        if (delta.sqrMagnitude <= _collectableData.CollectionRange * _collectableData.CollectionRange)
        {
            _isCollectable = true;
        }
    }
    private void HandleCollectInput()
    {
        if (_isCollectable) Collect();
    }
    private void HandleCollectInput(InputAction.CallbackContext context)
    {
        HandleCollectInput();
    }

    protected virtual void Notify()
    {
        if (_hasNotified) return;
        visuals.SetActive(true);
        StartCoroutine(DelayedNotification(_notificationPopupData.PopupTextData.Delay));
        _hasNotified = true;
    }

    protected virtual void Collect()
    {
        if (_wasCollected) return;

        if(_scoreCoroutine != null)
        {
            StopCoroutine(_scoreCoroutine);
            _scoreCoroutine = null;
        }
        
        GainScore();
        visuals.SetActive(false);
        LoadPopup_EC.RaiseEvent(_collectPopupData);
        _wasCollected = true;     
        OnDisable();
    }

    protected virtual void GainScore()
    {
        EventsRelay.OnScoreGain.Invoke(_score);
        AudioManager.Instance.PlayPointCollected();
    }

    private IEnumerator DelayedNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPopup_EC.RaiseEvent(_notificationPopupData);
        _scoreCoroutine = StartCoroutine(ScoreCoroutine());
    }

    private IEnumerator ScoreCoroutine()
    {
        WaitForSeconds timer = new (GameSettingsManager.GetFloat("Score Settings", "TimeForScoreDeduction", 1f));
        float safeTime = GameSettingsManager.GetFloat("Score Settings", "TimeForMaxScore", 10f);
        //safe time to get max score
        while (safeTime > 0)
        {
            if (GameManager.IsGamePaused) { yield return null; }
            safeTime -= Time.deltaTime;
            yield return null;
        }

        int scoreDeduction = GameSettingsManager.GetInt("Score Settings", "ScoreDeductionValue", 1);

        //losing score each period
        while (_score > 0)
        {
            if (GameManager.IsGamePaused) { yield return null; }

            _score -= scoreDeduction;
            yield return timer;
        }
    }
}
