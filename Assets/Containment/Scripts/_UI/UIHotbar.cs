using UnityEngine;
using UnityEngine.UI;

public class UIHotbar : MonoBehaviour
{
    public GameObject panel;
    public UIHotbarSlot slotPrefab;
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
            panel.SetActive(true);

            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.hotbar.size, content);

            // refresh all
            for (int i = 0; i < player.hotbar.size; ++i)
            {
                UIHotbarSlot slot = content.GetChild(i).GetComponent<UIHotbarSlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index

                ItemSlot itemSlot = player.hotbar.slots[i];

                // hotkey pressed and not typing in any input right now?
                // (and not while reloading, this would be unrealistic, feels weird
                //  and the reload bar UI needs selected weapon's reloadTime)
                if (Input.GetKeyDown(player.hotbar.keys[i]) &&
                    player.reloading.ReloadTimeRemaining() == 0 &&
                    !UIUtils.AnyInputActive())
                {
                    // empty? then select (for hand combat)
                    if (itemSlot.amount == 0)
                    {
                        player.hotbar.CmdSelect(i);
                    }
                    // usable item?
                    else if (itemSlot.item.data is UsableItem usable)
                    {
                        // use it directly or select the slot?
                        if (usable.useDirectly)
                            player.hotbar.CmdUseItem(i, player.look.lookPositionRaycasted);
                        else
                            player.hotbar.CmdSelect(i);
                    }
                }

                // overlay hotkey (without 'Alpha' etc.)
                slot.hotkeyText.text = player.hotbar.keys[i].ToString().Replace("Alpha", "");

                // is this slot selected?
                slot.selectionOutline.SetActive(i == player.hotbar.selection);

                if (itemSlot.amount > 0)
                {
                    // refresh valid item
                    slot.tooltip.enabled = true;
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
                    else slot.image.color = Color.white; // reset for no-durability items
                    slot.image.sprite = itemSlot.item.image;
                    // only build tooltip while it's actually shown. this
                    // avoids MASSIVE amounts of StringBuilder allocations.
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = itemSlot.ToolTip();
                    // cooldown if usable item
                    if (itemSlot.item.data is UsableItem)
                    {
                        UsableItem usable = (UsableItem)itemSlot.item.data;
                        float cooldown = player.GetItemCooldown(usable.cooldownCategory);
                        slot.cooldownCircle.fillAmount = usable.cooldown > 0 ? cooldown / usable.cooldown : 0;
                    }
                    else slot.cooldownCircle.fillAmount = 0;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    if (itemSlot.amount > 1) slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    // refresh invalid item
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.cooldownCircle.fillAmount = 0;
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
        else panel.SetActive(false);
    }
}
