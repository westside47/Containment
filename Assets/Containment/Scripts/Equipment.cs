﻿using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Inventory))]
public abstract class Equipment : ItemContainer, IHealthBonus, IHydrationBonus, INutritionBonus, ICombatBonus
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    public Health health;
    public Inventory inventory;

    // energy boni
    public int GetHealthBonus(int baseHealth)
    {
        // sum up manually. Linq.Sum() is HEAVY(!) on GC and performance (190 KB/call!)
        int bonus = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.CheckDurability())
                bonus += ((EquipmentItem)slot.item.data).healthBonus;
        return bonus;
    }
    public int GetHealthRecoveryBonus()
    {
        return 0;
    }
    public int GetHydrationBonus(int baseHydration)
    {
        // sum up manually. Linq.Sum() is HEAVY(!) on GC and performance (190 KB/call!)
        int bonus = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.CheckDurability())
                bonus += ((EquipmentItem)slot.item.data).hydrationBonus;
        return bonus;
    }
    public int GetHydrationRecoveryBonus()
    {
        return 0;
    }
    public int GetNutritionBonus(int baseNutrition)
    {
        // sum up manually. Linq.Sum() is HEAVY(!) on GC and performance (190 KB/call!)
        int bonus = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.CheckDurability())
                bonus += ((EquipmentItem)slot.item.data).nutritionBonus;
        return bonus;
    }
    public int GetNutritionRecoveryBonus()
    {
        return 0;
    }

    // combat boni
    public int GetDamageBonus()
    {
        // sum up manually. Linq.Sum() is HEAVY(!) on GC and performance (190 KB/call!)
        int bonus = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.CheckDurability())
                bonus += ((EquipmentItem)slot.item.data).damageBonus;
        return bonus;
    }
    public int GetDefenseBonus()
    {
        // sum up manually. Linq.Sum() is HEAVY(!) on GC and performance (190 KB/call!)
        int bonus = 0;
        foreach (ItemSlot slot in slots)
            if (slot.amount > 0 && slot.item.CheckDurability())
                bonus += ((EquipmentItem)slot.item.data).defenseBonus;
        return bonus;
    }
}