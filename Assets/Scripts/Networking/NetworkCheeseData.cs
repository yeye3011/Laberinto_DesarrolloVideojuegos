using System;
using System.Collections.Generic;

[Serializable]
public class NetworkCheeseData
{
    public int cheeseId;
    public string cheeseType;

    public float posX;
    public float posY;
    public float posZ;

    public float rotY;
}

[Serializable]
public class NetworkCheeseList
{
    public List<NetworkCheeseData> cheeses = new List<NetworkCheeseData>();
}

[System.Serializable]
public class SpeedEffectData
{
    public int playerId;
    public bool active;
}


[System.Serializable]
public class FreezeEffectData
{
    public int activatorPlayerId;
    public string activatorPlayerName;
    public int[] frozenPlayerIds;
    public bool active;
}

