using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="uSurvival Item/Equipment", order=999)]
public class EquipmentItem : UsableItem
{
    [Header("Equipment")]
    public int healthBonus;
    public int hydrationBonus;
    public int nutritionBonus;
    public int damageBonus;
    public int defenseBonus;

    // usage
    // -> can we equip this into any slot?
    public override Usability CanUseInventory(Player player, int inventoryIndex)
    {
        // check base usability first (cooldown etc.)
        Usability baseUsable = base.CanUseInventory(player, inventoryIndex);
        if (baseUsable != Usability.Usable)
            return baseUsable;

        return FindEquipableSlotFor(player.equipment, inventoryIndex) != -1
               ? Usability.Usable
               : Usability.Never;
    }
    public override Usability CanUseHotbar(Player player, int hotbarIndex, Vector3 lookAt) { return Usability.Never; }

    // can we equip this item into this specific equipment slot?
    public bool CanEquip(PlayerEquipment equipment, int inventoryIndex, int equipmentIndex)
    {
        string requiredCategory = equipment.slotInfo[equipmentIndex].requiredCategory;
        return requiredCategory != "" &&
               category.StartsWith(requiredCategory);
    }

    int FindEquipableSlotFor(PlayerEquipment equipment, int inventoryIndex)
    {
        for (int i = 0; i < equipment.slots.Count; ++i)
            if (CanEquip(equipment, inventoryIndex, i))
                return i;
        return -1;
    }

    public override void UseInventory(Player player, int inventoryIndex)
    {
        // find a slot that accepts this category, then equip it
        int slot = FindEquipableSlotFor(player.equipment, inventoryIndex);
        if (slot != -1)
        {
            // merge? e.g. ammo stack on ammo stack
            if (player.inventory.slots[inventoryIndex].amount > 0 && player.equipment.slots[slot].amount > 0 &&
                player.inventory.slots[inventoryIndex].item.Equals(player.equipment.slots[slot].item))
            {
                ItemSlot slotFrom = player.inventory.slots[inventoryIndex];
                ItemSlot slotTo = player.equipment.slots[slot];

                // merge from -> to
                // put as many as possible into 'To' slot
                int put = slotTo.IncreaseAmount(slotFrom.amount);
                slotFrom.DecreaseAmount(put);

                // put back into the lists
                player.equipment.slots[slot] = slotTo;
                player.inventory.slots[inventoryIndex] = slotFrom;
            }
            // swap?
            else
            {
                player.equipment.SwapInventoryEquip(inventoryIndex, slot);
            }
        }
    }
    public override void UseHotbar(Player player, int hotbarIndex, Vector3 lookAt) {}

    // tooltip
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{CATEGORY}", category);
        tip.Replace("{HEALTHBONUS}", healthBonus.ToString());
        tip.Replace("{HYDRATIONBONUS}", hydrationBonus.ToString());
        tip.Replace("{NUTRITIONBONUS}", nutritionBonus.ToString());
        tip.Replace("{DAMAGEBONUS}", damageBonus.ToString());
        tip.Replace("{DEFENSEBONUS}", defenseBonus.ToString());
        return tip.ToString();
    }
}
