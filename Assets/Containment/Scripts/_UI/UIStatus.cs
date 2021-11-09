// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;
using UnityEngine.UI;

public class UIStatus : MonoBehaviour
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

    public Text damageText;
    public Text defenseText;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            healthSlider.value = player.health.Percent();
            healthStatus.text = player.health.current + " / " + player.health.max;

            hydrationSlider.value = player.hydration.Percent();
            hydrationStatus.text = player.hydration.current + " / " + player.hydration.max;

            nutritionSlider.value = player.nutrition.Percent();
            nutritionStatus.text = player.nutrition.current + " / " + player.nutrition.max;

            temperatureSlider.value = player.temperature.Percent();
            float currentTemperature = player.temperature.current / 100f;
            float maxTemperature = player.temperature.max / 100f;
            string toStringFormat = "F" + temperatureDecimalDigits.ToString(); // "F2" etc.
            temperatureStatus.text = currentTemperature.ToString(toStringFormat) + " / " +
                                     maxTemperature.ToString(toStringFormat) + " " +
                                     temperatureUnit;

            enduranceSlider.value = player.endurance.Percent();
            enduranceStatus.text = player.endurance.current + " / " + player.endurance.max;

            damageText.text = player.combat.damage.ToString();
            defenseText.text = player.combat.defense.ToString();
        }
    }
}
