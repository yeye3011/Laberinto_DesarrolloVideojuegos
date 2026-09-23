using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSession : MonoBehaviour
{
    public static NetworkSession Instance { get; private set; }

    [Serializable]
    public class PlayerData
    {
        public int playerId;
        public string playerName;
        public int spawnIndex;
    }

    [Serializable]
    private class PlayerDataList
    {
        public List<PlayerData> players =
            new List<PlayerData>();
    }

    [Header("Datos de la partida")]
    [SerializeField] private int localPlayerId;

    [SerializeField]
    private List<PlayerData> players =
        new List<PlayerData>();

    public int LocalPlayerId => localPlayerId;

    public List<PlayerData> Players => players;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetLocalPlayerId(int id)
    {
        localPlayerId = id;

        Debug.Log(
            "ID local establecido: " + localPlayerId
        );
    }

    public void ClearPlayers()
    {
        players.Clear();
    }

    public void AddPlayer(
        int id,
        string name,
        int spawnIndex)
    {
        players.Add(new PlayerData
        {
            playerId = id,
            playerName = name,
            spawnIndex = spawnIndex
        });
    }

    public PlayerData GetPlayer(int id)
    {
        return players.Find(
            player => player.playerId == id
        );
    }

    public string GetPlayersJson()
    {
        PlayerDataList data =
            new PlayerDataList();

        data.players.AddRange(players);

        return JsonUtility.ToJson(data);
    }

    public void LoadPlayersJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning(
                "No se recibieron datos de jugadores."
            );

            return;
        }

        PlayerDataList data =
            JsonUtility.FromJson<PlayerDataList>(json);

        if (data == null || data.players == null)
        {
            Debug.LogWarning(
                "No se pudo interpretar la lista de jugadores."
            );

            return;
        }

        players.Clear();

        players.AddRange(data.players);

        Debug.Log(
            "Datos de jugadores recibidos: " +
            players.Count
        );

        foreach (PlayerData player in players)
        {
            Debug.Log(
                "Jugador " +
                player.playerId +
                " - " +
                player.playerName +
                " - Spawn " +
                player.spawnIndex
            );
        }
    }
}
