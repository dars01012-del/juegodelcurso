using UnityEngine;
using UnityEngine.InputSystem;

public class Gatherinput : MonoBehaviour
{
    private Controls controls;
    [SerializeField] private float _valueX;
    [SerializeField] private bool _isJumping;
    [SerializeField] private bool _isShooting;

    public float ValueX => _valueX;
    public bool IsJumping { get => _isJumping; set => _isJumping = value; }
    public bool IsShooting { get => _isShooting; set => _isShooting = value; }
    public bool IsJumpHeld => controls != null && controls.Player.Jump.IsPressed();

    private void EnsureControls()
    {
        if (controls == null)
            controls = new Controls();
    }

    private void OnEnable()
    {
        EnsureControls();

        controls.Player.Move.performed += StartMove;
        controls.Player.Move.canceled += StopMove;
        controls.Player.Jump.performed += StartJump;
        controls.Player.Jump.canceled += StopJump;
        controls.Player.shoot.performed += StartShoot;
        controls.Player.Enable();
    }

    private void StartMove(InputAction.CallbackContext context)
    {
        _valueX = context.ReadValue<float>();
    }

    private void StopMove(InputAction.CallbackContext context)
    {
        _valueX = 0f;
    }

    private void StartJump(InputAction.CallbackContext context)
    {
        _isJumping = true;
    }

    private void StopJump(InputAction.CallbackContext context)
    {
        _isJumping = false;
    }

    private void StartShoot(InputAction.CallbackContext context)
    {
        _isShooting = true;
    }

    private void OnDisable()
    {
        if (controls == null)
            return;

        controls.Player.Move.performed -= StartMove;
        controls.Player.Move.canceled -= StopMove;
        controls.Player.Jump.performed -= StartJump;
        controls.Player.Jump.canceled -= StopJump;
        controls.Player.shoot.performed -= StartShoot;
        controls.Player.Disable();
    }

    private void OnDestroy()
    {
        controls?.Dispose();
        controls = null;
    }
}
