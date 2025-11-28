using NaGaDeMon.Features.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.Player.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 720f;

        private Rigidbody2D rb;
        private Camera cam;
        private InputSystem_Actions input;

        private Vector2 moveInput;
        private Vector2 lookInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            cam = Camera.main;

            // Reuse shared InputSystem instance
            input = InputContextManager.Instance.input;

            // Movement input
            input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            input.Player.Move.canceled += _ => moveInput = Vector2.zero;

            // Look input
            input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            input.Player.Look.canceled += _ => lookInput = Vector2.zero;
        }

        private void OnEnable() => input.Enable();
        private void OnDisable() => input.Disable();

        private void FixedUpdate()
        {
            HandleMovement();
            HandleRotation();
        }

        private void HandleMovement()
        {
            // Use Move input directly
            rb.linearVelocity = moveInput * moveSpeed;
        }

        private void HandleRotation()
        {
            Vector2 aimDir = Vector2.zero;

            // Gamepad look
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f)
            {
                aimDir = Gamepad.current.rightStick.ReadValue();
            }
            else
            {
                // Mouse look (2D world)
                Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                aimDir = mouseWorld - (Vector2)transform.position;
            }

            if (aimDir.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
                float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
                rb.MoveRotation(newAngle);
            }
        }
    }
}
