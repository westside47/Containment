﻿using UnityEngine;
using UnityEngine.UI;

public class UIChat : MonoBehaviour
{
    public static UIChat singleton;
    public GameObject panel;
    public InputField messageInput;
    public Transform content;
    public ScrollRect scrollRect;
    public KeyCode[] activationKeys = {KeyCode.Return, KeyCode.KeypadEnter};
    public int keepHistory = 100; // only keep 'n' messages

    bool eatActivation;

    public UIChat()
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
            panel.SetActive(true);

            // character limit
            messageInput.characterLimit = player.chat.maxLength;

            // activation (ignored once after deselecting, so it doesn't immediately
            // activate again)
            if (Utils.AnyKeyDown(activationKeys) && !eatActivation)
                messageInput.Select();
            eatActivation = false;

            // end edit listener
            messageInput.onEndEdit.SetListener((value) => {
                // submit key pressed? then submit and set new input text
                if (Utils.AnyKeyDown(activationKeys))
                {
                    string newinput = player.chat.OnSubmit(value);
                    messageInput.text = newinput;
                    messageInput.MoveTextEnd(false);
                    eatActivation = true;
                }

                // unfocus the whole chat in any case. otherwise we would scroll or
                // activate the chat window when doing wsad movement afterwards
                UIUtils.DeselectCarefully();
            });
        }
        else panel.SetActive(false);
    }

    void AutoScroll()
    {
        // update first so we don't ignore recently added messages, then scroll
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void AddMessage(ChatMessage message)
    {
        // delete an old message if we have too many
        if (content.childCount >= keepHistory)
            Destroy(content.GetChild(0).gameObject);

        // instantiate and initialize text prefab
        GameObject go = Instantiate(message.textPrefab, content.transform, false);
        go.GetComponent<Text>().text = message.Construct();
        go.GetComponent<UIChatEntry>().message = message;

        AutoScroll();
    }

    // called by chat entries when clicked
    public void OnEntryClicked(UIChatEntry entry)
    {
        // any reply prefix?
        if (!string.IsNullOrWhiteSpace(entry.message.replyPrefix))
        {
            // set text to reply prefix
            messageInput.text = entry.message.replyPrefix;

            // activate
            messageInput.Select();

            // move cursor to end (doesn't work in here, needs small delay)
            Invoke(nameof(MoveTextEnd), 0.1f);
        }
    }

    void MoveTextEnd()
    {
        messageInput.MoveTextEnd(false);
    }
}
