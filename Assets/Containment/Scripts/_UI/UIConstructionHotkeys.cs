using UnityEngine;
using UnityEngine.UI;

public class UIConstructionHotkeys : MonoBehaviour
{
    public GameObject panel;
    public Text rotationText;

    void Update()
    {
        // holding a structure?
        Player player = Player.localPlayer;
        if (player != null)
        {
            rotationText.text = player.construction.rotationKey + " - Rotate";
            panel.SetActive(player.construction.GetCurrentStructure() != null);
        }
        else panel.SetActive(false);
    }
}
