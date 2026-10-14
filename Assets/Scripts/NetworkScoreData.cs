using System;
using System.Collections.Generic;

[Serializable]
public class PlayerScoreData
{
    public int playerId;
    public string playerName;
    public int score;
}

[Serializable]
public class PlayerScoreList
{
    public List<PlayerScoreData> players = new List<PlayerScoreData>();
}
