using System.Collections.Generic;
using NaGaDeMon.Features.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NaGaDeMon.Features.Player
{
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private LayerMask interactableMask;

        [Header("UI")]
        [SerializeField] private ItemWheelUI wheelPrefab;
        [SerializeField] private float wheelHoverSelectTime = 0.4f;

        private InputSystem_Actions input;
        private Camera cam;
        private PlayerInventory playerInventory;

        private IInteractable currentTarget;

        // Dumping
        private bool isHoldingDump = false;
        private bool isDumping = false;
        private ItemDefinition selectedDumpItem;

        // Taking
        private bool isTaking = false;

        [SerializeField] ItemWheelUI activeWheelUI;

        private void Awake()
        {
            input = InputContextManager.Instance.input;
            cam = Camera.main;
            playerInventory = GetComponent<PlayerInventory>();
        }

        private void OnEnable()
        {
            // Dump (Left Click)
            input.Player.Dump.started += OnDumpStarted;
            input.Player.Dump.canceled += OnDumpCanceled;

            // Take (Right Click)
            input.Player.Take.performed += OnTakePerformed;
            input.Player.Take.canceled += OnTakeCanceled;
        }

        private void OnDisable()
        {
            input.Player.Dump.started -= OnDumpStarted;
            input.Player.Dump.canceled -= OnDumpCanceled;

            input.Player.Take.performed -= OnTakePerformed;
            input.Player.Take.canceled -= OnTakeCanceled;
        }

        private void Update()
        {
            // If wheel is open, freeze targeting
            if (activeWheelUI != null)
            {
                HandleDump();
                HandleTake();
                return;
            }

            // ❗ NEW: freeze targeting during actual transfer
            if (isDumping || isTaking)
            {
                HandleDump();
                HandleTake();
                return;
            }

            // Normal mode
            UpdateHoverTarget();
            HandleDump();
            HandleTake();
        }

        // ------------------------------------------------------------
        // HOVERING
        // ------------------------------------------------------------
        private void UpdateHoverTarget()
        {
            Vector2 mousePos = input.Player.Point.ReadValue<Vector2>();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z)
            );
            worldPos.z = 0;

            Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.25f, interactableMask);
            IInteractable newTarget = hit ? hit.GetComponent<IInteractable>() : null;

            if (newTarget != currentTarget)
            {
                currentTarget?.OnHoverExit();
                currentTarget = newTarget;
                currentTarget?.OnHoverEnter();
            }
        }

        // ------------------------------------------------------------
        // TAKE (RIGHT CLICK)
        // ------------------------------------------------------------
        private void OnTakePerformed(InputAction.CallbackContext ctx)
        {
            if (currentTarget == null) return;
            if (DistToTarget(currentTarget) > interactRange) return;

            isTaking = true;
        }

        private void OnTakeCanceled(InputAction.CallbackContext ctx)
        {
            isTaking = false;
        }

        private void HandleTake()
        {
            if (!isTaking || currentTarget == null)
                return;

            if (DistToTarget(currentTarget) > interactRange)
            {
                isTaking = false;
                return;
            }

            // Continuous interaction
            currentTarget.OnInteractHoldRight(playerInventory);
        }

        // ------------------------------------------------------------
        // DUMP (LEFT CLICK)
        // ------------------------------------------------------------
        private void OnDumpStarted(InputAction.CallbackContext ctx)
        {
            if (currentTarget == null) return;
            if (DistToTarget(currentTarget) > interactRange) return;

            List<ItemDefinition> defs = playerInventory.GetAllDefinitions();
            if (defs.Count == 0) return;

            isHoldingDump = true;

            // Only one item → no need for wheel
            if (defs.Count == 1)
            {
                selectedDumpItem = defs[0];
                isDumping = true;
                return;
            }

            // MULTIPLE ITEMS → open wheel
            Vector2 screenPos = input.Player.Point.ReadValue<Vector2>();

            activeWheelUI = wheelPrefab;
            activeWheelUI.gameObject.SetActive(true);

            activeWheelUI.Open(
                screenPos,
                defs,
                wheelHoverSelectTime,
                OnItemSelectedFromWheel
            );
        }

        private void OnDumpCanceled(InputAction.CallbackContext ctx)
        {
            isHoldingDump = false;
            isDumping = false;
            selectedDumpItem = null;

            if (activeWheelUI != null)
                activeWheelUI.gameObject.SetActive(false);
            activeWheelUI = null;
        }

        private void OnItemSelectedFromWheel(ItemDefinition def)
        {
            selectedDumpItem = def;
            isDumping = true;

            if (activeWheelUI != null)
                activeWheelUI.gameObject.SetActive(false);
            activeWheelUI = null;
        }

        private void HandleDump()
        {
            if (!isDumping || selectedDumpItem == null || currentTarget == null)
                return;

            if (DistToTarget(currentTarget) > interactRange)
            {
                isDumping = false;
                return;
            }

            // 🛑 STOP DUMPING WHEN PLAYER RUNS OUT OF THIS ITEM
            if (playerInventory.GetAmount(selectedDumpItem) <= 0f)
            {
                isDumping = false;
                return;
            }

            // Continuous interaction (1 item per frame)
            currentTarget.OnInteractHoldLeft(playerInventory, selectedDumpItem);
        }

        // ------------------------------------------------------------
        // UTIL
        // ------------------------------------------------------------
        private float DistToTarget(IInteractable t)
        {
            return Vector2.Distance(
                transform.position,
                ((MonoBehaviour)t).transform.position
            );
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
