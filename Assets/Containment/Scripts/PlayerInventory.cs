using UnityEngine;
using Mirror;

[RequireComponent(typeof(Health))]
public class PlayerInventory : Inventory
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    [Header("Components")]
    public Player player;

    [Header("Inventory")]
    public int size = 10;
    public ScriptableItemAndAmount[] defaultItems;

    public KeyCode[] splitKeys = {KeyCode.LeftShift, KeyCode.RightShift};

    [Header("Item Drops")]
    public float dropRadius = 1;
    public int dropSolverAttempts = 3; // attempts to drop without being behind a wall, etc.

    [Command]
    public void CmdSwapInventoryInventory(int fromIndex, int toIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and that they are not equal
        if (player.health.current > 0 &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            // swap them
            ItemSlot temp = slots[fromIndex];
            slots[fromIndex] = slots[toIndex];
            slots[toIndex] = temp;
        }
    }

    [Command]
    public void CmdInventorySplit(int fromIndex, int toIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and that they are not equal
        if (player.health.current > 0 &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            // slotFrom needs at least two to split, slotTo has to be empty
            ItemSlot slotFrom = slots[fromIndex];
            ItemSlot slotTo = slots[toIndex];
            if (slotFrom.amount >= 2 && slotTo.amount == 0) {
                // split them serversided (has to work for even and odd)
                slotTo = slotFrom; // copy the value

                slotTo.amount = slotFrom.amount / 2;
                slotFrom.amount -= slotTo.amount; // works for odd too

                // put back into the list
                slots[fromIndex] = slotFrom;
                slots[toIndex] = slotTo;
            }
        }
    }

    [Command]
    public void CmdInventoryMerge(int fromIndex, int toIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and that they are not equal
        if (player.health.current > 0 &&
            0 <= fromIndex && fromIndex < slots.Count &&
            0 <= toIndex && toIndex < slots.Count &&
            fromIndex != toIndex)
        {
            // both items have to be valid
            ItemSlot slotFrom = slots[fromIndex];
            ItemSlot slotTo = slots[toIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                // make sure that items are the same type
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                if (slotFrom.item.Equals(slotTo.item))
                {
                    // merge from -> to
                    // put as many as possible into 'To' slot
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);

                    // put back into the list
                    slots[fromIndex] = slotFrom;
                    slots[toIndex] = slotTo;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcUsedItem(Item item)
    {
        // validate
        if (item.data is UsableItem usable)
        {
            usable.OnUsedInventory(player);
        }
    }

    [Command]
    public void CmdUseItem(int index)
    {
        // validate
        // note: checks durability only if it should be used (if max > 0)
        if (player.health.current > 0 &&
            0 <= index && index < slots.Count &&
            slots[index].amount > 0 &&
            slots[index].item.CheckDurability() &&
            slots[index].item.data is UsableItem usable)
        {
            // use item
            // note: we don't decrease amount / destroy in all cases because
            // some items may swap to other slots in .Use()
            if (usable.CanUseInventory(player, index) == Usability.Usable)
            {
                // .Use might clear the slot, so we backup the Item first for the Rpc
                Item item = slots[index].item;
                usable.UseInventory(player, index);
                RpcUsedItem(item);
            }
        }
    }

    [Server]
    public void DropItem(Item item, int amount)
    {
        // drop at random point on navmesh that is NOT behind a wall
        // -> dropping behind a wall is just bad gameplay
        // -> on navmesh because that's the easiest way to find the ground
        //    without accidentally raycasting ourselves or something else
        Vector3 position = Utils.ReachableRandomUnitCircleOnNavMesh(transform.position, dropRadius, dropSolverAttempts);

        // drop
        GameObject go = Instantiate(item.data.drop.gameObject, position, Quaternion.identity);
        ItemDrop drop = go.GetComponent<ItemDrop>();
        drop.item = item;
        drop.amount = amount;
        NetworkServer.Spawn(go);
    }

    [Server]
    public void DropItemAndClearSlot(int index)
    {
        // drop and remove from inventory
        ItemSlot slot = slots[index];
        DropItem(slot.item, slot.amount);
        slot.amount = 0;
        slots[index] = slot;
    }

    [Command]
    public void CmdDropItem(int index)
    {
        // validate
        if (player.health.current > 0 &&
            0 <= index && index < slots.Count && slots[index].amount > 0)
        {
            DropItemAndClearSlot(index);
        }
    }

    // durability //////////////////////////////////////////////////////////////
    public void OnReceivedDamage(Entity attacker, int damage)
    {
        // reduce durability in each item
        for (int i = 0; i < slots.Count; ++i)
        {
            if (slots[i].amount > 0)
            {
                ItemSlot slot = slots[i];
                slot.item.durability = Mathf.Clamp(slot.item.durability - damage, 0, slot.item.maxDurability);
                slots[i] = slot;
            }
        }
    }

    // death & respawn /////////////////////////////////////////////////////////
    // drop all items on death, so others can loot us
    [Server]
    public void OnDeath()
    {
        for (int i = 0; i < slots.Count; ++i)
            if (slots[i].amount > 0)
                DropItemAndClearSlot(i);
    }

    // we don't clear items on death so that others can still loot us. we clear
    // them on respawn.
    [Server]
    public void OnRespawn()
    {
        // for each slot: make empty slot or default item if any
        for (int i = 0; i < slots.Count; ++i)
            slots[i] = i < defaultItems.Length ? new ItemSlot(new Item(defaultItems[i].item), defaultItems[i].amount) : new ItemSlot();
    }

    // drag & drop /////////////////////////////////////////////////////////////
    void OnDragAndDrop_InventorySlot_InventorySlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo

        // merge? (just check equality, rest is done server sided)
        if (slots[slotIndices[0]].amount > 0 && slots[slotIndices[1]].amount > 0 &&
            slots[slotIndices[0]].item.Equals(slots[slotIndices[1]].item))
        {
            CmdInventoryMerge(slotIndices[0], slotIndices[1]);
        }
        // split?
        else if (Utils.AnyKeyPressed(splitKeys))
        {
            CmdInventorySplit(slotIndices[0], slotIndices[1]);
        }
        // dropped ammo onto weapon to load it?
        else if (slots[slotIndices[0]].amount > 0 && slots[slotIndices[1]].amount > 0 &&
                 player.reloading.CanLoadAmmoIntoWeapon(slots[slotIndices[0]], slots[slotIndices[1]].item))
        {
            player.reloading.CmdReloadWeaponInInventory(slotIndices[0], slotIndices[1]);
        }
        // swap?
        else
        {
            CmdSwapInventoryInventory(slotIndices[0], slotIndices[1]);
        }
    }

    void OnDragAndClear_InventorySlot(int slotIndex)
    {
        CmdDropItem(slotIndex);
    }

    // validation
    void OnValidate()
    {
        // it's easy to set a default item and forget to set amount from 0 to 1
        // -> let's do this automatically.
        for (int i = 0; i < defaultItems.Length; ++i)
            if (defaultItems[i].item != null && defaultItems[i].amount == 0)
                defaultItems[i].amount = 1;
    }
}