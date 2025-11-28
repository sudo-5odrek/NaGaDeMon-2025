using Floating_Text_Service;
using NaGaDeMon.Features.Inventory;
using UnityEngine;

namespace NaGaDeMon.Features.Environment
{
    [DisallowMultipleComponent]
    public class MiningNode : MonoBehaviour
    {
        [Header("Node Settings")]
        [Tooltip("The item produced when this node is mined.")]
        public ItemDefinition resourceItem;

        public FloatingTextStyle textStyle;

        [Tooltip("How many items are produced per tick.")]
        public int resourcePerTick = 1;

        [Tooltip("How long between each mined item (in seconds).")]
        public float miningInterval = 2f;

        [Tooltip("How long the player must stand still before mining starts.")]
        public float activationDelay = 1f;

        [Header("Resource Pool")]
        [Tooltip("If true, this node will never deplete.")]
        public bool isInfinite = false;

        [Tooltip("Total number of resources available in this node (ignored if infinite).")]
        public int maxResources = 100;

        private int currentResources;
        private bool isDepleted;

        private void Awake()
        {
            currentResources = maxResources;
        }

        /// <summary>
        /// Attempts to mine from the node. Returns true if successful and provides the mined ItemDefinition and amount.
        /// </summary>
        public bool TryMine(out ItemDefinition item, out int amount, out FloatingTextStyle miningTextStyle)
        {
            item = null;
            amount = 0;
            miningTextStyle = null;

            // Node cannot produce if it has no item or is depleted (unless infinite)
            if (resourceItem == null || (isDepleted && !isInfinite))
                return false;

            // Provide mined resources
            item = resourceItem;
            amount = resourcePerTick;
            miningTextStyle = textStyle;

            // Only reduce stock if not infinite
            if (!isInfinite)
            {
                currentResources -= resourcePerTick;
                if (currentResources <= 0)
                {
                    currentResources = 0;
                    isDepleted = true;
                }
            }

            return true;
        }

        public bool IsDepleted => isDepleted && !isInfinite;
    }
}