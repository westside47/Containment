using UnityEngine;
using UnityEngine.UI;

public class UIMainPanel : MonoBehaviour
{
    // singleton to access it from player scripts without FindObjectOfType
    public static UIMainPanel singleton;

    public KeyCode hotKey = KeyCode.Tab;
    public GameObject panel;
    public Button quitButton;

    public UIMainPanel()
    {
        // assign singleton only once (to work with DontDestroyOnLoad when
        // using Zones / switching scenes)
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            // show "(5)Quit" if we can't log out during combat
            // -> CeilToInt so that 0.1 shows as '1' and not as '0'
            string quitPrefix = "";
            if (player.remainingLogoutTime > 0)
                quitPrefix = "(" + Mathf.CeilToInt((float)player.remainingLogoutTime) + ") ";
            quitButton.GetComponent<UIShowToolTip>().text = quitPrefix + "Quit";
            quitButton.interactable = player.remainingLogoutTime == 0;
            quitButton.onClick.SetListener(NetworkManagerSurvival.Quit);
        }
        // hide if server stopped and player disconnected
        else panel.SetActive(false);
    }

    public void Show()
    {
        panel.SetActive(true);
    }
}
