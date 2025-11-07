using Player;
using UnityEngine;
using Interface;

namespace Building.Turrets
{
    [RequireComponent(typeof(Turret))]
    public class TurretInventory : MonoBehaviour, IInteractable
    {
        [Header("Ammo Settings")]
        [Tooltip("The resource ID used as ammo (e.g. 'ammo', 'energy_cell').")]
        [SerializeField] private string ammoResourceId = "ammo";
        [Tooltip("How much total ammo this turret can hold.")]
        [SerializeField] private float maxAmmoCapacity = 50f;
        [Tooltip("How many units of ammo are consumed per shot.")]
        [SerializeField] private float ammoPerShot = 1f;

        [Header("Transfer Settings")]
        [Tooltip("Time (seconds) between each transferred unit when holding interact.")]
        [SerializeField] private float transferInterval = 0.2f; // 1 every 0.2s = 5/sec
        private float transferTimerLeft;
        private float transferTimerRight;

        [Header("Debug")]
        [SerializeField] private float currentAmmo;

        // --- Core ---
        public Inventory Inventory { get; private set; }
        private Turret turretController;

        private void Awake()
        {
            Inventory = new Inventory(maxAmmoCapacity);
            turretController = GetComponent<Turret>();
            Inventory.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            if (Inventory != null)
                Inventory.OnInventoryChanged -= OnInventoryChanged;
        }

        // ------------------------------------------------------------
        //  AMMO CONSUMPTION
        // ------------------------------------------------------------

        public bool TryConsumeAmmo()
        {
            if (!Inventory.Contains(ammoResourceId, ammoPerShot))
                return false;

            Inventory.Remove(ammoResourceId, ammoPerShot);
            return true;
        }

        private void OnInventoryChanged()
        {
            currentAmmo = Inventory.Get(ammoResourceId);
        }

        public float GetAmmoFraction()
        {
            return maxAmmoCapacity <= 0 ? 0f : currentAmmo / maxAmmoCapacity;
        }

        // ------------------------------------------------------------
        //  INTERACTION IMPLEMENTATION
        // ------------------------------------------------------------

        public void OnHoverEnter()
        {
            // 🔹 Show tooltip (handled by UI system)
            //TooltipSystem.Show($"Turret\n[LMB] Add Ammo\n[RMB] Collect Ammo");
        }

        public void OnHoverExit()
        {
            //TooltipSystem.Hide();
        }

        public void OnInteractHoldLeft(PlayerInventory playerInventory)
        {
            // Continuous tick-based transfer: 1 unit every interval
            transferTimerLeft += Time.deltaTime;
            if (transferTimerLeft < transferInterval)
                return;

            transferTimerLeft -= transferInterval;

            float removed = playerInventory.Inventory.Remove(ammoResourceId, 1f);
            float added = Inventory.Add(ammoResourceId, removed);

            if (added > 0f)
            {
                Debug.Log($"+1 {ammoResourceId} added to turret.");
                //TooltipSystem.UpdateText($"Ammo: {currentAmmo + 1}/{maxAmmoCapacity}");
            }
        }

        public void OnInteractHoldRight(PlayerInventory playerInventory)
        {
            transferTimerRight += Time.deltaTime;
            if (transferTimerRight < transferInterval)
                return;

            transferTimerRight -= transferInterval;

            float removed = Inventory.Remove(ammoResourceId, 1f);
            float added = playerInventory.Inventory.Add(ammoResourceId, removed);

            if (added > 0f)
            {
                Debug.Log($"-1 {ammoResourceId} collected from turret.");
                //TooltipSystem.UpdateText($"Ammo: {currentAmmo - 1}/{maxAmmoCapacity}");
            }
        }
    }
}
