using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private Vector3EventChannel playerMoved_EC;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float pitchSpeed;
    [SerializeField] private Rigidbody rb;

    private float _turnDirection;
    private float _pitchDirection;

    private InputActions _actions;

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
    }

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Turn.performed -= HandleTurnInput;
        _actions.Player.Turn.canceled -= HandleTurnInput;
        _actions.Player.Pitch.performed -= HandlePitchInput;
        _actions.Player.Pitch.canceled -= HandlePitchInput;
    }

    void FixedUpdate()
    {
        Rotate();
        Move();
    }

    private void Rotate()
    {
        Vector3 eulerAngleVelocity = new Vector3(pitchSpeed * _pitchDirection, turnSpeed * _turnDirection, 0f);
        Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void Move()
    {
        Vector3 move = moveSpeed * Time.fixedDeltaTime * transform.forward;
        rb.MovePosition(transform.position + move);
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
}
