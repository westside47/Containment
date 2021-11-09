﻿// We implemented a chat system that works directly with UNET. The chat supports
// different channels that can be used to communicate with other players:
//
// - **Local Chat:** by default, all messages that don't start with a **/** are
// addressed to the local chat. If one player writes a local message, then all
// players around him _(all observers)_ will be able to see the message.
// - **Whisper Chat:** a player can write a private message to another player by
// using the **/ name message** format.
// - **Info Chat:** the info chat can be used by the server to notify all
// players about important news. The clients won't be able to write any info
// messages.
//
// _Note: the channel names, colors and commands can be edited in the Inspector_
using System;
using UnityEngine;
using Mirror;

[Serializable]
public class ChannelInfo
{
    public string command; // /w etc.
    public string identifierOut; // for sending
    public string identifierIn; // for receiving
    public GameObject textPrefab;

    public ChannelInfo(string command, string identifierOut, string identifierIn, GameObject textPrefab)
    {
        this.command = command;
        this.identifierOut = identifierOut;
        this.identifierIn = identifierIn;
        this.textPrefab = textPrefab;
    }
}

[Serializable]
public struct ChatMessage
{
    public string sender;
    public string identifier;
    public string message;
    public string replyPrefix; // copied to input when clicking the message
    public GameObject textPrefab;

    public ChatMessage(string sender, string identifier, string message, string replyPrefix, GameObject textPrefab)
    {
        this.sender = sender;
        this.identifier = identifier;
        this.message = message;
        this.replyPrefix = replyPrefix;
        this.textPrefab = textPrefab;
    }

    // construct the message
    public string Construct()
    {
        return "<b>" + sender + identifier + ":</b> " + message;
    }
}

public class PlayerChat : NetworkBehaviourNonAlloc
{
    // channels
    [Header("Channels")]
    public ChannelInfo whisper = new ChannelInfo("/w", "(TO)", "(FROM)", null);
    public ChannelInfo local = new ChannelInfo("", "", "", null);
    public ChannelInfo info = new ChannelInfo("", "(Info)", "(Info)", null);

    [Header("Other")]
    public int maxLength = 70;

    public override void OnStartLocalPlayer()
    {
        // test messages
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, "  Use /w NAME to whisper", "",  info.textPrefab));
        UIChat.singleton.AddMessage(new ChatMessage("Someone", whisper.identifierIn, "Are you there?", "/w Someone ",  whisper.textPrefab));
        UIChat.singleton.AddMessage(new ChatMessage("Someone", local.identifierIn, "Hello!", "/w Someone ",  local.textPrefab));
    }

    // submit tries to send the string and then returns the new input text
    [Client]
    public string OnSubmit(string text)
    {
        // not empty and not only spaces?
        if (!string.IsNullOrWhiteSpace(text))
        {
            // command in the commands list?
            // note: we don't do 'break' so that one message could potentially
            //       be sent to multiple channels (see mmorpg local chat)
            string lastcommand = "";
            if (text.StartsWith(whisper.command))
            {
                // whisper
                string[] parsed = ParsePM(whisper.command, text);
                string user = parsed[0];
                string msg = parsed[1];
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(msg))
                {
                    if (user != name)
                    {
                        lastcommand = whisper.command + " " + user + " ";
                        CmdMsgWhisper(user, msg);
                    }
                    else print("cant whisper to self");
                }
                else print("invalid whisper format: " + user + "/" + msg);
            }
            else if (!text.StartsWith("/"))
            {
                // local chat is special: it has no command
                lastcommand = "";
                CmdMsgLocal(text);
            }

            // input text should be set to lastcommand
            return lastcommand;
        }

        // input text should be cleared
        return "";
    }

    // parse a message of form "/command message"
    static string ParseGeneral(string command, string msg)
    {
		// return message without command prefix (if any)
        return msg.StartsWith(command + " ") ? msg.Substring(command.Length + 1) : "";
    }

    static string[] ParsePM(string command, string pm)
    {
        // parse to /w content
        string content = ParseGeneral(command, pm);

        // now split the content in "user msg"
        if (content != "")
        {
            // find the first space that separates the name and the message
            int i = content.IndexOf(" ");
            if (i >= 0)
            {
                string user = content.Substring(0, i);
                string msg = content.Substring(i+1);
                return new string[] {user, msg};
            }
        }
        return new string[] {"", ""};
    }

    // networking //////////////////////////////////////////////////////////////
    [Command]
    void CmdMsgLocal(string message)
    {
        if (message.Length > maxLength) return;

        // it's local chat, so let's send it to all observers via ClientRpc
        RpcMsgLocal(name, message);
    }

    [Command]
    void CmdMsgWhisper(string playerName, string message)
    {
        if (message.Length > maxLength) return;

        // find the player with that name
        if (Player.onlinePlayers.TryGetValue(playerName, out Player player))
        {
            // receiver gets a 'from' message, sender gets a 'to' message
            // (call TargetRpc on that GameObject for that connection)
            player.chat.TargetMsgWhisperFrom(name, message);
            TargetMsgWhisperTo(playerName, message);
        }
    }

    // send a global info message to everyone
    [Server]
    public void SendGlobalMessage(string message)
    {
        foreach (Player player in Player.onlinePlayers.Values)
            player.chat.TargetMsgInfo(message);
    }

    // message handlers ////////////////////////////////////////////////////////
    [TargetRpc]
    public void TargetMsgWhisperFrom(string sender, string message)
    {
        // add message with identifierIn
        string identifier = whisper.identifierIn;
        string reply = whisper.command + " " + sender + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, whisper.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgWhisperTo(string receiver, string message)
    {
        // add message with identifierOut
        string identifier = whisper.identifierOut;
        string reply = whisper.command + " " + receiver + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(receiver, identifier, message, reply, whisper.textPrefab));
    }

    [ClientRpc]
    public void RpcMsgLocal(string sender, string message)
    {
        // add message with identifierIn or Out depending on who sent it
        string identifier = sender != name ? local.identifierIn : local.identifierOut;
        string reply = whisper.command + " " + sender + " "; // whisper
        UIChat.singleton.AddMessage(new ChatMessage(sender, identifier, message, reply, local.textPrefab));
    }

    [TargetRpc]
    public void TargetMsgInfo(string message)
    {
        UIChat.singleton.AddMessage(new ChatMessage("", info.identifierIn, message, "", info.textPrefab));
    }
}
