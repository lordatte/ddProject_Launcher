using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private float moveSpeed = 5f;   

    private Inputs_actions inputs;
    private Vector2 moveInput;

    private void Awake()
    {
        inputs = new Inputs_actions();
    }

    private void OnEnable()
    {
        inputs.Player.Enable();
        inputs.Player.Move.performed += OnMove;
        inputs.Player.Move.canceled  += OnMove; 
    }

    private void OnDisable()
    {
        inputs.Player.Move.performed -= OnMove;
        inputs.Player.Move.canceled  -= OnMove;
        inputs.Player.Disable();
    }

    private void OnDestroy()
    {
        inputs?.Dispose();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>(); 
        
    }

    private void FixedUpdate()
    {
        rb2d.velocity = moveInput * moveSpeed;
    }
}