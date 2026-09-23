using System;
using UnityEngine;

[Serializable]
public class PlayerNetworkState
{
    public int playerId;

    public float posX;
    public float posY;
    public float posZ;

    public float rotY;

    public int state;
}