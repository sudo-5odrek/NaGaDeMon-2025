using UnityEngine;

namespace Building.Turrets
{
    [RequireComponent(typeof(Turret))]
    public class TurretInventory : MonoBehaviour
    {
        [Header("Ammo Settings")]
        public string ammoResourceId = "ammo";
        public float maxAmmoCapacity = 50f;
        public float ammoPerShot = 1f;

        [Header("Debug")]
        [SerializeField] private float currentAmmo;

        public Inventory.Inventory Inventory { get; private set; }
        private Turret turretController;

        private void Awake()
        {
            Inventory = new Inventory.Inventory(maxAmmoCapacity);
            turretController = GetComponent<Turret>();
            Inventory.OnInventoryChanged += UpdateAmmo;
        }

        private void OnDestroy()
        {
            if (Inventory != null)
                Inventory.OnInventoryChanged -= UpdateAmmo;
        }

        private void UpdateAmmo()
        {
            currentAmmo = Inventory.Get(ammoResourceId);
        }

        public bool TryConsumeAmmo()
        {
            if (!Inventory.Contains(ammoResourceId, ammoPerShot))
                return false;

            Inventory.Remove(ammoResourceId, ammoPerShot);
            return true;
        }

        public float GetAmmoFraction()
        {
            return maxAmmoCapacity <= 0 ? 0f : currentAmmo / maxAmmoCapacity;
        }
    }
}