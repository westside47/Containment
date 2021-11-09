// Inventory & Equip both use slots and some common functions. might as well
// abstract them to save code.
using UnityEngine;
using Mirror;

public abstract class ItemContainer : NetworkBehaviourNonAlloc
{
    // the slots
    public SyncListItemSlot slots = new SyncListItemSlot();

    // helper function to find an item in the slots
    public int GetItemIndexByName(string itemName)
    {
        // (avoid FindIndex to minimize allocations)
        for (int i = 0; i < slots.Count; ++i)
        {
            ItemSlot slot = slots[i];
            if (slot.amount > 0 && slot.item.name == itemName)
                return i;
        }
        return -1;
    }
}
