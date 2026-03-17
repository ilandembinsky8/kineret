using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI.Table;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private TransformEventChannel playerMoved_EC;
    [SerializeField] private TransformEventChannel cameraPitched_EC;
    [SerializeField] private FloatEventChannel moveSpeedChange_EC;

    [SerializeField] private float legMoveSpeed;
    [SerializeField] private float maxRoll;
    [SerializeField] private float rollDuration;
    [SerializeField] private float yawSpeed;
    [SerializeField] private float pitchSpeed;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private Transform pitchBody;
    [SerializeField] private Transform yawBody;
    [SerializeField] private Transform rollBody;



    private float _accelerationDirection;
    private float _accelerationPercentage = 0.05f;
    private float _maxMoveSpeedPercentage = 0.2f; //Move to INI
    [SerializeField] private float _moveSpeed;
    private float _yawDirection;
    private float _pitchDirection;

    private InputActions _actions;
    private bool _isPaused;
    private float _currentRoll;
    private float _priorYawDirection;

    private void Awake()
    {
        _actions = new InputActions();
        Roll();
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.Turn.performed += HandleTurnInput;
        _actions.Player.Turn.canceled += HandleTurnInput;
        _actions.Player.Pitch.performed += HandlePitchInput;
        _actions.Player.Pitch.canceled += HandlePitchInput;
        _actions.Player.Accelerate.performed += HandleSpeedInput;
        _actions.Player.Accelerate.canceled += HandleSpeedInput;

        EventsRelay.OnGamePause += HandleGamePause;
        moveSpeedChange_EC.OnEventRaised += ChangeMoveSpeedByLeg;
    }

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Turn.performed -= HandleTurnInput;
        _actions.Player.Turn.canceled -= HandleTurnInput;
        _actions.Player.Pitch.performed -= HandlePitchInput;
        _actions.Player.Pitch.canceled -= HandlePitchInput;
        _actions.Player.Accelerate.performed -= HandleSpeedInput;
        _actions.Player.Accelerate.canceled -= HandleSpeedInput;

        EventsRelay.OnGamePause -= HandleGamePause;
        moveSpeedChange_EC.OnEventRaised -= ChangeMoveSpeedByLeg;
    }

    void Update()
    {
        if (_isPaused) return; 
        Yaw();
        Pitch();
        Roll();
        Move();     
    }

    private void Roll()
    {
        if(_yawDirection == _priorYawDirection) return; 

        rollBody.DOKill();

        if (_yawDirection != 0 )
        {
            Vector3 roll = new Vector3(0, 0, maxRoll * Mathf.Sign(-_yawDirection));
            rollBody.DOLocalRotate(roll, rollDuration);
        }
        else
        {
            rollBody.DOLocalRotate(Vector3.zero, rollDuration);
        }
        _priorYawDirection = _yawDirection;
        //_currentRoll = Mathf.Clamp(_currentRoll + Time.deltaTime * rollSpeed * -_yawDirection,-maxRoll,maxRoll);
        //rollBody.localRotation = Quaternion.Euler(0f, 0f, _currentRoll);
    }
    private void Yaw()
    {
        yawBody.Rotate(transform.up, Time.deltaTime * yawSpeed * _yawDirection, Space.World);
    }
    private void Pitch()
    {
        pitchBody.Rotate(pitchBody.right, Time.deltaTime * pitchSpeed * _pitchDirection, Space.World);
        cameraPitched_EC.RaiseEvent(pitchBody);
    }

    private void Move()
    {
        Debug.Log(_accelerationDirection);
        _moveSpeed += legMoveSpeed * _accelerationPercentage * _accelerationDirection * Time.deltaTime;
        _moveSpeed = Mathf.Clamp(_moveSpeed, legMoveSpeed * (1 - _maxMoveSpeedPercentage), legMoveSpeed * (1 + _maxMoveSpeedPercentage));
        transform.Translate(_moveSpeed * Time.deltaTime * (transform.InverseTransformDirection(pitchBody.forward)));
        playerMoved_EC.RaiseEvent(transform);
    }

    private void HandlePitchInput(InputAction.CallbackContext context)
    {
        _pitchDirection = context.ReadValue<float>();
    }

    private void HandleTurnInput(InputAction.CallbackContext context)
    {
        _yawDirection = context.ReadValue<float>();
    }

    private void HandleSpeedInput(InputAction.CallbackContext context)
    {
        _accelerationDirection = context.ReadValue<float>();
    }

    private void HandleGamePause(bool isPaused)
    {
        _isPaused = isPaused;
    }
    private void ChangeMoveSpeedByLeg(float newMoveSpeed)
    {
        legMoveSpeed = newMoveSpeed;
        _moveSpeed = legMoveSpeed;
        Debug.Log("changed speed to moveSpeed");
    }
}
