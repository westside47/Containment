using UnityEngine;
using UnityEngine.UI;

public class UIHud : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text healthStatus;
    public Slider hydrationSlider;
    public Text hydrationStatus;
    public Slider nutritionSlider;
    public Text nutritionStatus;
    public Slider temperatureSlider;
    public Text temperatureStatus;
    public string temperatureUnit = "°C";
    public int temperatureDecimalDigits = 1;
    public Slider enduranceSlider;
    public Text enduranceStatus;
    public Text ammoText;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            panel.SetActive(true);

            // health
            healthSlider.value = player.health.Percent();
            healthStatus.text = player.health.current + " / " + player.health.max;

            // hydration
            hydrationSlider.value = player.hydration.Percent();
            hydrationStatus.text = player.hydration.current + " / " + player.hydration.max;

            // nutrition
            nutritionSlider.value = player.nutrition.Percent();
            nutritionStatus.text = player.nutrition.current + " / " + player.nutrition.max;

            // temperature (scaled down, see Temperature script)
            temperatureSlider.value = player.temperature.Percent();
            float currentTemperature = player.temperature.current / 100f;
            float maxTemperature = player.temperature.max / 100f;
            string toStringFormat = "F" + temperatureDecimalDigits.ToString(); // "F2" etc.
            temperatureStatus.text = currentTemperature.ToString(toStringFormat) + " / " +
                                     maxTemperature.ToString(toStringFormat) + " " +
                                     temperatureUnit;

            // endurance
            enduranceSlider.value = player.endurance.Percent();
            enduranceStatus.text = player.endurance.current + " / " + player.endurance.max;

            // ammo
            ItemSlot slot = player.hotbar.slots[player.hotbar.selection];
            if (slot.amount > 0 && slot.item.data is RangedWeaponItem itemData)
            {
                if (itemData.requiredAmmo != null)
                {
                    ammoText.text = slot.item.ammo + " / " + itemData.magazineSize;
                }
                else ammoText.text = "0 / 0";
            }
            else ammoText.text = "0 / 0";
        }
        else panel.SetActive(false);
    }
}