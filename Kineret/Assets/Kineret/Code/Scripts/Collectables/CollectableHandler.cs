using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollectableHandler : MonoBehaviour
{
    [SerializeField] protected PopupDataEventChannel LoadPopup_EC;
    [SerializeField] protected TransformEventChannel playerMoved_EC;
    [SerializeField] protected IntEventChannel gotScore_EC;

    [SerializeField] protected CollectableData _collectableData;
    [SerializeField] protected PopupData _notificationPopupData;
    [SerializeField] protected PopupData _collectPopupData;

    [SerializeField] protected GameObject visuals;

    protected bool _hasNotified;
    protected bool _wasCollected;
    protected bool _isCollectable;

    private InputActions _actions;

    private void Awake()
    {
        _actions = new InputActions();
    }
    protected virtual void Start()
    {
        visuals.SetActive(false);
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.Collect.performed += HandleCollectInput;
        playerMoved_EC.OnEventRaised += HandlePlayerMoved;
    }

    protected void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Collect.performed -= HandleCollectInput;
        playerMoved_EC.OnEventRaised -= HandlePlayerMoved;
    }

    private void OnDrawGizmos()
    {
        if(_collectableData.NotificationRange > 0)
        {
            Handles.color = Color.red;
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
        _notificationPopupData.PopupTextData = notificationPopupData;
        _collectPopupData.PopupTextData = collectPopupData;
    }

    protected virtual void HandlePlayerMoved(Transform playerTransform)
    {
        Vector3 playerPositionXZ = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
        Vector3 collectablePositionXZ = new Vector3(transform.position.x, 0f, transform.position.z);

        Vector3 delta = playerPositionXZ - collectablePositionXZ;

        CheckNotifyRange(delta);
        CheckCollectRange(delta);          
    }
    protected virtual void CheckNotifyRange(Vector3 delta)
    {
        if (delta.sqrMagnitude <= _collectableData.NotificationRange * _collectableData.NotificationRange)
        {
            Notify();
        }
    }
    protected virtual void CheckCollectRange(Vector3 delta)
    {
        _isCollectable = false;
        if (delta.sqrMagnitude <= _collectableData.CollectionRange * _collectableData.CollectionRange)
        {
            _isCollectable = true;
        }
    }

    private void HandleCollectInput(InputAction.CallbackContext context)
    {
        if(_isCollectable) Collect();
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

        gotScore_EC.RaiseEvent(_collectableData.MaxScore);
        visuals.SetActive(false);
        LoadPopup_EC.RaiseEvent(_collectPopupData);
        _wasCollected = true;
        OnDisable();
    }

    private IEnumerator DelayedNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPopup_EC.RaiseEvent(_notificationPopupData);
    }
}
