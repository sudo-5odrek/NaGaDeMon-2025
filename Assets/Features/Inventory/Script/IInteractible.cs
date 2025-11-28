using NaGaDeMon.Features.Inventory;
using NaGaDeMon.Features.Player;

namespace NaGaDeMon.Features.Inventory
{
    public interface IInteractable
    {
        void OnHoverEnter();
        void OnHoverExit();

        /// <summary>
        /// Dump items into this interactable.
        /// Now includes selected ItemDefinition from wheel.
        /// </summary>
        void OnInteractHoldLeft(PlayerInventory inventory, ItemDefinition item);

        /// <summary>
        /// Take items from this interactable.
        /// No change needed.
        /// </summary>
        void OnInteractHoldRight(PlayerInventory inventory);
    }
}