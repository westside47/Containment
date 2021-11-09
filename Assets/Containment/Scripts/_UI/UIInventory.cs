using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour
{
    public UIInventorySlot slotPrefab;
    public Transform content;

    [Header("Durability Colors")]
    public Color brokenDurabilityColor = Color.red;
    public Color lowDurabilityColor = Color.magenta;
    [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.inventory.slots.Count, content);

            // refresh all items
            for (int i = 0; i < player.inventory.slots.Count; ++i)
            {
                UIInventorySlot slot = content.GetChild(i).GetComponent<UIInventorySlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                ItemSlot itemSlot = player.inventory.slots[i];

                if (itemSlot.amount > 0)
                {
                    // refresh valid item
                    int icopy = i; // needed for lambdas, otherwise i is Count
                    slot.button.onClick.SetListener(() => {
                        // check durability & usability
                        if (itemSlot.item.CheckDurability() &&
                            itemSlot.item.data is UsableItem usable &&
                            usable.CanUseInventory(player, icopy) == Usability.Usable)
                            player.inventory.CmdUseItem(icopy);
                    });
                    slot.tooltip.enabled = true;
                    // only build tooltip while it's actually shown. this
                    // avoids MASSIVE amounts of StringBuilder allocations.
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = itemSlot.ToolTip();
                    slot.dragAndDropable.dragable = true;
                    // use durability colors?
                    if (itemSlot.item.maxDurability > 0)
                    {
                        if (itemSlot.item.durability == 0)
                            slot.image.color = brokenDurabilityColor;
                        else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                            slot.image.color = lowDurabilityColor;
                        else
                            slot.image.color = Color.white;
                    }
                    else slot.image.color = Color.white; // reset for non-durability items
                    slot.image.sprite = itemSlot.item.image;
                    // cooldown if usable item
                    if (itemSlot.item.data is UsableItem usable2)
                    {
                        float cooldown = player.GetItemCooldown(usable2.cooldownCategory);
                        slot.cooldownCircle.fillAmount = usable2.cooldown > 0 ? cooldown / usable2.cooldown : 0;
                    }
                    else slot.cooldownCircle.fillAmount = 0;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    // refresh invalid item
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.cooldownCircle.fillAmount = 0;
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
    }
}
