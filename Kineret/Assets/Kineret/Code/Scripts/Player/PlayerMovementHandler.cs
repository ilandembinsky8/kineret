using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private Vector3EventChannel playerMoved_EC;
    [SerializeField] private BoolEventChannel GamePause_EC;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float pitchSpeed;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private GameObject pitchBody;

    private float _turnDirection;
    private float _pitchDirection;

    private InputActions _actions;
    private bool _isPaused;

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

        GamePause_EC.OnEventRaised += HandlePause;
}

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Turn.performed -= HandleTurnInput;
        _actions.Player.Turn.canceled -= HandleTurnInput;
        _actions.Player.Pitch.performed -= HandlePitchInput;
        _actions.Player.Pitch.canceled -= HandlePitchInput;

        GamePause_EC.OnEventRaised -= HandlePause;
    }

    void Update()
    {
        if (_isPaused) return;
        Rotate();
        Move();
    }

    private void Rotate()
    {
        transform.Rotate(transform.up, Time.deltaTime * turnSpeed * _turnDirection,Space.World);
        pitchBody.transform.Rotate(pitchBody.transform.right, Time.deltaTime * pitchSpeed * _pitchDirection, Space.World);
    }

    private void Move()
    {
        transform.Translate(moveSpeed * Time.deltaTime * (transform.InverseTransformDirection(pitchBody.transform.forward)));
        playerMoved_EC.RaiseEvent(transform.position);
    }

    private void HandlePitchInput(InputAction.CallbackContext context)
    {
        _pitchDirection = context.ReadValue<float>();
    }

    private void HandleTurnInput(InputAction.CallbackContext context)
    {
        _turnDirection = context.ReadValue<float>();
    }

    private void HandlePause(bool value)
    {
        _isPaused = value;
    }
}
