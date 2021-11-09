using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class UIRespawn : MonoBehaviour
{
    public GameObject panel;
    public Text timeText;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player && player.health.current == 0)
        {
            panel.SetActive(true);

            // calculate the respawn time remaining for the client
            double remaining = player.respawning.respawnTimeEnd - NetworkTime.time;
            timeText.text = remaining.ToString("F0");
        }
        else panel.SetActive(false);
    }
}
