using UnityEngine;
using UnityEngine.InputSystem;

public class Gatherinput : MonoBehaviour
{
  private Controls controls;
 [SerializeField] private float _valueX;

    public float ValueX { get => _valueX; }

    private void Awake()
    {
      controls = new Controls();

    }
    private void OnEnable()
    {
        controls.Player.Move.performed += startMove;
        controls.Player.Move.canceled += StopMove;
      controls.Player.Enable();
    }
    private void startMove(InputAction.CallbackContext context)
    {
        _valueX = context.ReadValue<float>();
        }
        private void StopMove (InputAction.CallbackContext context) {
            _valueX = 0;
        }
    private void OnDisable()
    {
        controls.Player.Move.performed -= startMove;
        controls.Player.Move.canceled -= StopMove;
      controls.Player.Disable();
    }

}
