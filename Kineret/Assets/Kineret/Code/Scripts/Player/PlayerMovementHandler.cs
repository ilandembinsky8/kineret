using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI.Table;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private TransformEventChannel playerMoved_EC;
    [SerializeField] private TransformEventChannel cameraPitched_EC;
    [SerializeField] private BoolEventChannel GamePause_EC;
    [SerializeField] private FloatEventChannel moveSpeedChange_EC;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxRoll;
    [SerializeField] private float rollSpeed;
    [SerializeField] private float yawSpeed;
    [SerializeField] private float pitchSpeed;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private GameObject pitchBody;
    [SerializeField] private GameObject yawBody;

    private float _yawDirection;
    private float _pitchDirection;

    private InputActions _actions;
    private bool _isPaused;
    private float _currentRoll;

    private void Awake()
    {
        _actions = new InputActions();      
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.Turn.performed += HandleTurnInput;
        _actions.Player.Turn.canceled += HandleTurnInput;
        _actions.Player.Pitch.performed += HandlePitchInput;
        _actions.Player.Pitch.canceled += HandlePitchInput;

        GamePause_EC.OnEventRaised += HandleGamePause;
        moveSpeedChange_EC.OnEventRaised += ChangeMoveSpeedByLeg;
    }

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Turn.performed -= HandleTurnInput;
        _actions.Player.Turn.canceled -= HandleTurnInput;
        _actions.Player.Pitch.performed -= HandlePitchInput;
        _actions.Player.Pitch.canceled -= HandlePitchInput;

        GamePause_EC.OnEventRaised -= HandleGamePause;
        moveSpeedChange_EC.OnEventRaised -= ChangeMoveSpeedByLeg;
    }

    void Update()
    {
        if (_isPaused) return;
        //Roll();
        Yaw();
        Pitch();
        Move();
        
    }

    private void Roll()
    {
        _currentRoll = Mathf.Clamp(_currentRoll + Time.deltaTime * rollSpeed * -_yawDirection,-maxRoll,maxRoll);
        transform.rotation = Quaternion.Euler(0f, 0f, _currentRoll); 
    }
    private void Yaw()
    {
        yawBody.transform.Rotate(transform.up, Time.deltaTime * yawSpeed * _yawDirection, Space.World);
    }
    private void Pitch()
    {
        pitchBody.transform.Rotate(pitchBody.transform.right, Time.deltaTime * pitchSpeed * _pitchDirection, Space.World);
        cameraPitched_EC.RaiseEvent(pitchBody.transform);
    }

    private void Move()
    {
        transform.Translate(moveSpeed * Time.deltaTime * (transform.InverseTransformDirection(pitchBody.transform.forward)));
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

    private void HandleGamePause(bool isPaused)
    {
        _isPaused = isPaused;
    }
    private void ChangeMoveSpeedByLeg(float newMoveSpeed)
    {
        moveSpeed = newMoveSpeed;
        Debug.Log("changed speed to moveSpeed");
    }
}
