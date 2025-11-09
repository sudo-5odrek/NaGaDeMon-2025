using Interface;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private LayerMask interactableMask;

        private InputSystem_Actions input;
        private Camera cam;
        private PlayerInventory playerInventory;

        private IInteractable currentTarget;
        
        private bool isHolding;
        private bool isLeftInteraction;

        private void Awake()
        {
            input = InputContextManager.Instance.input;
            cam = Camera.main;
            playerInventory = GetComponent<PlayerInventory>();
        }

        private void OnEnable()
        {
            input.Player.Dump.started += OnLeftInteractStarted;
            input.Player.Dump.canceled += OnInteractCanceled;
            input.Player.Take.started += OnRightInteractStarted;
            input.Player.Take.canceled += OnInteractCanceled;
        }

        private void OnDisable()
        {
            input.Player.Dump.started -= OnLeftInteractStarted;
            input.Player.Dump.canceled -= OnInteractCanceled;
            input.Player.Take.started -= OnRightInteractStarted;
            input.Player.Take.canceled -= OnInteractCanceled;
        }

        private void Update()
        {
            UpdateHoverTarget();
            HandleHoldInteraction();
        }

        // ------------------------------------------------------------
        //  HOVER + RANGE
        // ------------------------------------------------------------

        private void UpdateHoverTarget()
        {
            Vector2 mousePos = input.Player.Point.ReadValue<Vector2>();
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
            worldPos.z = 0;

            Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.2f, interactableMask);
            IInteractable newTarget = hit ? hit.GetComponent<IInteractable>() : null;

            if (newTarget != currentTarget)
            {
                currentTarget?.OnHoverExit();
                currentTarget = newTarget;
                currentTarget?.OnHoverEnter();
            }
        }

        // ------------------------------------------------------------
        //  INPUT EVENTS
        // ------------------------------------------------------------

        private void OnLeftInteractStarted(InputAction.CallbackContext ctx)
        {
            if (currentTarget == null) return;
            isLeftInteraction = true;
            isHolding = true;
        }

        private void OnRightInteractStarted(InputAction.CallbackContext ctx)
        {
            if (currentTarget == null) return;
            isLeftInteraction = false;
            isHolding = true;
        }

        private void OnInteractCanceled(InputAction.CallbackContext ctx)
        {
            isHolding = false;
        }

        // ------------------------------------------------------------
        //  HOLD INTERACTION
        // ------------------------------------------------------------

        private void HandleHoldInteraction()
        {
            if (!isHolding || currentTarget == null)
                return;
            
            // Range guard
            var targetMb = (MonoBehaviour)currentTarget;
            float dist = Vector2.Distance(transform.position, targetMb.transform.position);
            
            if (dist > interactRange)
            {
                currentTarget.OnHoverExit();
                currentTarget = null;
                isHolding = false;
                return;
            }
            
            Debug.Log($"Holding {(isLeftInteraction ? "LMB" : "RMB")} on {currentTarget}");
            
            // 🔁 Continuous transfer every frame while held.
            // Implementers (e.g., TurretInventory) should use Time.deltaTime and a transferRate
            // to do "X units per second".
            if (isLeftInteraction)
                currentTarget.OnInteractHoldLeft(playerInventory);
            else
                currentTarget.OnInteractHoldRight(playerInventory);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
