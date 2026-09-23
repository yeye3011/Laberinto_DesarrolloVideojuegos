using System;

[Serializable]
public class NetworkMessage
{
    public string type;
    public int playerId;
    public string playerName;

    public string data;
}
