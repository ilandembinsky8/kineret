using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine;
using TMPro;

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
    [SerializeField] public Transform YawBody;
    [SerializeField] private Transform rollBody;

    private float _keyBoardAccelerationDirection;
    private float _accelerationDirection;
    private float _accelerationPercentage = 0.05f;
    private float _maxMoveSpeedPercentage = 0.2f;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private TextMeshProUGUI moveSpeedText;

    private float _keyBoardYawDirection;
    private float _keyBoardPitchDirection;

    private float _yawDirection;
    private float _pitchDirection;

    private InputActions _actions;
    private float _priorYawDirection;

    private float _windForcePercentage = 0.1f;
    private Vector3 _windDirection;
    private bool _isWindEnabled;

    private void Awake()
    {
        _actions = new InputActions();
        maxRoll = GameSettingsManager.GetFloat("Player Settings", "MaxRoll  ", maxRoll);
        rollDuration = GameSettingsManager.GetFloat("Player Settings", "RollDuration   ", rollDuration);
        yawSpeed = GameSettingsManager.GetFloat("Player Settings", "YawSpeed   ", yawSpeed);
        pitchSpeed = GameSettingsManager.GetFloat("Player Settings", "PitchSpeed   ", pitchSpeed);
        _accelerationPercentage = GameSettingsManager.GetFloat("Player Settings", "AccelerationPercentage", _accelerationPercentage);
        _maxMoveSpeedPercentage = GameSettingsManager.GetFloat("Player Settings", "MaxMoveSpeedPercentage", _maxMoveSpeedPercentage);
        _windForcePercentage = GameSettingsManager.GetFloat("Player Settings", "WindForcePercentage", _windForcePercentage);
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

        moveSpeedChange_EC.OnEventRaised += ChangeMoveSpeedByLeg;

        EventsRelay.OnWindEvent += EnableWind;
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

        moveSpeedChange_EC.OnEventRaised -= ChangeMoveSpeedByLeg;

        EventsRelay.OnWindEvent -= EnableWind;
    }

    void Update()
    {
        if (GameManager.IsGamePaused) return;

        Yaw();
        Pitch();
        Roll();
        Move();
    }
    /// <summary>
    /// Rotating the plane left or right around the vertical axis.
    /// </summary>
    private void Yaw()
    {
        _yawDirection = JoystickManager.GetHorizontalAxis() + _keyBoardYawDirection;

        if (Mathf.Abs(_yawDirection) > JoystickManager.StickDeadzone)
        {
            YawBody.Rotate(transform.up, Time.deltaTime * yawSpeed * _yawDirection, Space.World);
        }
    }
    /// <summary>
    /// Tilting the nose up or down
    /// </summary>
    private void Pitch()
    {
        _pitchDirection = JoystickManager.GetVerticalAxis() + _keyBoardPitchDirection;

        if (Mathf.Abs(_pitchDirection) > JoystickManager.StickDeadzone)
        {
            pitchBody.Rotate(pitchBody.right, Time.deltaTime * pitchSpeed * _pitchDirection, Space.World);
            cameraPitched_EC.RaiseEvent(pitchBody);
        }
    }
    /// <summary>
    /// Tilting the wings left or right
    /// </summary>
    private void Roll()
    {
        if (_yawDirection == _priorYawDirection) return;

        rollBody.DOKill();

        if (_yawDirection != 0 && Mathf.Abs(_yawDirection) > JoystickManager.StickDeadzone)
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


    private void Move()
    {
        _accelerationDirection = Input.GetAxis(JoystickManager.JoystickControls.MiniVerticalAxis) + _keyBoardAccelerationDirection;

        _moveSpeed += legMoveSpeed * _accelerationPercentage * _accelerationDirection * Time.deltaTime;
        _moveSpeed = Mathf.Clamp(_moveSpeed, legMoveSpeed * (1 - _maxMoveSpeedPercentage), legMoveSpeed * (1 + _maxMoveSpeedPercentage));

        UpdateSpeedText();

        transform.Translate(_moveSpeed * Time.deltaTime * transform.InverseTransformDirection(pitchBody.forward));

        if (_isWindEnabled)
        {
            transform.Translate(legMoveSpeed * (1 + _windForcePercentage) * Time.deltaTime * _windDirection);
        }

        playerMoved_EC.RaiseEvent(transform);
    }

    private void HandlePitchInput(InputAction.CallbackContext context)
    {
        _keyBoardPitchDirection = context.ReadValue<float>();
    }

    private void HandleTurnInput(InputAction.CallbackContext context)
    {
        _keyBoardYawDirection = context.ReadValue<float>();
    }

    private void HandleSpeedInput(InputAction.CallbackContext context)
    {
        _keyBoardAccelerationDirection = context.ReadValue<float>();
    }

    private void ChangeMoveSpeedByLeg(float newMoveSpeed)
    {
        legMoveSpeed = newMoveSpeed;
        _moveSpeed = legMoveSpeed;
        UpdateSpeedText();
    }

    private void EnableWind(ChallengeType type, bool isEnabled)
    {
        _isWindEnabled = isEnabled;

        if (!_isWindEnabled)
        {
            _windDirection = Vector3.zero;
        }
        else
        {
            if (type == ChallengeType.SideWind)
            {
                //_windDirection = -YawBody.forward;
                //_windDirection = -YawBody.right;
                _windDirection = YawBody.right;
            }
        }
    }

    private void UpdateSpeedText()
    {
        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"{_moveSpeed:F2}";
        }
    }

}