// Contains all the network messages that we need.
using System.Collections.Generic;
using Mirror;

// client to server ////////////////////////////////////////////////////////////
public struct LoginMsg : NetworkMessage
{
    public string account;
    public string password;
    public string version;
}

public struct CharacterCreateMsg : NetworkMessage
{
    public string name;
    public int classIndex;
}

public partial struct CharacterSelectMsg : NetworkMessage
{
    public int value;
}

public partial struct CharacterDeleteMsg : NetworkMessage
{
    public int value;
}

// server to client ////////////////////////////////////////////////////////////
// we need an error msg packet because we can't use TargetRpc with the Network-
// Manager, since it's not a MonoBehaviour.
public struct ErrorMsg : NetworkMessage
{
    public string text;
    public bool causesDisconnect;
}

public partial struct LoginSuccessMsg : NetworkMessage
{
}

public struct CharactersAvailableMsg : NetworkMessage
{
    public struct CharacterPreview
    {
        public string name;
        public string className; // = the prefab name
    }
    public CharacterPreview[] characters;

    // load method in this class so we can still modify the characters structs
    // in the addon hooks
    public void Load(List<Player> players)
    {
        // we only need name and class for our UI
        // (avoid Linq because it is HEAVY(!) on GC and performance)
        characters = new CharacterPreview[players.Count];
        for (int i = 0; i < players.Count; ++i)
        {
            Player player = players[i];
            characters[i] = new CharacterPreview
            {
                name = player.name,
                className = player.className
            };
        }
    }
}