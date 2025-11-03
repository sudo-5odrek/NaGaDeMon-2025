using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;  // an empty child where bullets come from
    [SerializeField] float shootCooldown = 0.25f;

    float shootTimer;
    
    Rigidbody2D rb;
    InputSystem_Actions controls;
    Vector2 moveInput;
    Vector2 lookInput;
    Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        controls = new InputSystem_Actions();

        // Movement input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => moveInput = Vector2.zero;

        // Look input (mouse or right stick)
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += _ => lookInput = Vector2.zero;
        
        controls.Player.Attack.performed += _ => TryShoot();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        // --- Determine facing direction ---
        Vector2 aimDir = Vector2.zero;

        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f)
        {
            // Right stick controls look direction
            aimDir = Gamepad.current.rightStick.ReadValue();
        }
        else
        {
            // Mouse controls look direction
            Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            aimDir = mousePos - (Vector2)transform.position;
        }

        if (aimDir.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f; // adjust offset if sprite faces right
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
        
        shootTimer -= Time.fixedDeltaTime;

    }
    
    void TryShoot()
    {
        if (shootTimer > 0f) return;
        shootTimer = shootCooldown;

        Vector2 aimDir;

        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f)
            aimDir = Gamepad.current.rightStick.ReadValue();
        else
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            aimDir = mousePos - (Vector2)firePoint.position;
        }

        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity)
            .GetComponent<Bullet>();
        bullet.Init(aimDir);
    }
}
