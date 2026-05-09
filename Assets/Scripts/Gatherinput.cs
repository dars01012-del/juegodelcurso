using UnityEngine;
using UnityEngine.InputSystem;

public class Gatherinput : MonoBehaviour
{
  private Controls controls;
  private float valueX;

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
        valueX = context.ReadValue<float>();
        }
        private void StopMove (InputAction.CallbackContext context) {
            valueX = 0;
        }
    private void OnDisable()
    {
        controls.Player.Move.performed -= startMove;
        controls.Player.Move.canceled -= StopMove;
      controls.Player.Disable();
    }

}
