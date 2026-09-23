using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly List<GameObject> spawnedPlayers = new List<GameObject>();
    private NetworkPlayerReceiver networkPlayerReceiver;

    private void Start()
    {
        networkPlayerReceiver =
            FindFirstObjectByType<NetworkPlayerReceiver>();

        if (networkPlayerReceiver == null)
        {
            Debug.LogError(
                "PlayerSpawner: no se encontró NetworkPlayerReceiver."
            );
        }

        SpawnPlayers();
    }

    public void SpawnPlayers()
    {
        ClearPlayers();

        if (NetworkSession.Instance == null)
        {
            Debug.LogError(
                "PlayerSpawner: no existe NetworkSession."
            );

            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError(
                "PlayerSpawner: falta asignar Player Prefab."
            );

            return;
        }

        if (spawnPoints == null || spawnPoints.Length < 4)
        {
            Debug.LogError(
                "PlayerSpawner: se necesitan 4 Spawn Points."
            );

            return;
        }

        List<NetworkSession.PlayerData> players = NetworkSession.Instance.Players;

        if (players == null || players.Count == 0)
        {
            Debug.LogError(
                "PlayerSpawner: no hay jugadores en NetworkSession."
            );

            return;
        }

        List<int> usedSpawnIndexes = new List<int>();

        foreach (NetworkSession.PlayerData playerData in players)
        {
            if (playerData.spawnIndex < 0 ||
                playerData.spawnIndex >= spawnPoints.Length)
            {
                Debug.LogError(
                    "Spawn inválido para el jugador " +
                    playerData.playerId
                );

                continue;
            }

            if (usedSpawnIndexes.Contains(playerData.spawnIndex))
            {
                Debug.LogError(
                    "ERROR: el Spawn Point " +
                    playerData.spawnIndex +
                    " fue asignado a más de un jugador."
                );

                continue;
            }

            usedSpawnIndexes.Add(playerData.spawnIndex);

            Transform spawnPoint = spawnPoints[playerData.spawnIndex];

            GameObject player =
                Instantiate(
                    playerPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation
                );

            if (networkPlayerReceiver != null)
            {
                networkPlayerReceiver.RegisterPlayer(
                    playerData.playerId,
                    player
                );
            }

            PlayerIdentity identity = player.GetComponent<PlayerIdentity>();

            if (identity != null)
            {
                identity.SetIdentity(
                    playerData.playerId,
                    playerData.playerName
                );
            }

            PlayerLocal localPlayer = player.GetComponent<PlayerLocal>();

            if (localPlayer != null)
            {
                bool isLocal =
                    playerData.playerId ==
                    NetworkSession.Instance.LocalPlayerId;

                localPlayer.SetLocalPlayer(isLocal);
            }

            spawnedPlayers.Add(player);
        }

        Debug.Log(
            "Jugadores creados desde NetworkSession: " +
            spawnedPlayers.Count
        );
    }

    public void ClearPlayers()
    {
        foreach (GameObject player in spawnedPlayers)
        {
            if (player != null)
            {
                Destroy(player);
            }
        }

        spawnedPlayers.Clear();
    }
}