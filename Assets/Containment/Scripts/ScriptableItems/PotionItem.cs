using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName="uSurvival Item/Potion", order=999)]
public class PotionItem : UsableItem
{
    [Header("Potion")]
    public int usageHealth;
    public int usageHydration;
    public int usageNutrition;

    // note: no need to overwrite CanUse functions. simply check cooldowns in base.

    void ApplyEffects(Player player)
    {
        player.health.current += usageHealth;
        player.hydration.current += usageHydration;
        player.nutrition.current += usageNutrition;
    }

    public override void UseInventory(Player player, int inventoryIndex)
    {
        // call base function to start cooldown
        base.UseInventory(player, inventoryIndex);

        ApplyEffects(player);

        // decrease amount
        ItemSlot slot = player.inventory.slots[inventoryIndex];
        slot.DecreaseAmount(1);
        player.inventory.slots[inventoryIndex] = slot;
    }

    public override void UseHotbar(Player player, int hotbarIndex, Vector3 lookAt)
    {
        // call base function to start cooldown
        base.UseHotbar(player, hotbarIndex, lookAt);

        ApplyEffects(player);

        // decrease amount
        ItemSlot slot = player.hotbar.slots[hotbarIndex];
        slot.DecreaseAmount(1);
        player.hotbar.slots[hotbarIndex] = slot;
    }

    // tooltip
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{USAGEHEALTH}", usageHealth.ToString());
        tip.Replace("{USAGEHYDRATION}", usageHydration.ToString());
        tip.Replace("{USAGENUTRITION}", usageNutrition.ToString());
        return tip.ToString();
    }
}
