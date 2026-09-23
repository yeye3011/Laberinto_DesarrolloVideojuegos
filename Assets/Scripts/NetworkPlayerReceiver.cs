using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerReceiver : MonoBehaviour
{
    private LobbyClient lobbyClient;
    private LobbyServer lobbyServer;

    private readonly Dictionary<int, PlayerNetworkState> latestStates = new Dictionary<int, PlayerNetworkState>();

    private readonly Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    private void Start()
    {
        lobbyClient =
            FindFirstObjectByType<LobbyClient>();

        lobbyServer =
            FindFirstObjectByType<LobbyServer>();

        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerStateReceived +=
                ReceivePlayerState;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerStateReceived +=
                ReceivePlayerState;
        }

        if (lobbyClient == null &&
            lobbyServer == null)
        {
            Debug.LogError(
                "NetworkPlayerReceiver: no se encontró " +
                "LobbyClient ni LobbyServer."
            );
        }
    }

    private void ReceivePlayerState(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        PlayerNetworkState state =
            JsonUtility.FromJson<PlayerNetworkState>(json);

        if (state == null)
            return;

        lock (latestStates)
        {
            latestStates[state.playerId] = state;
        }
    }

    private void Update()
    {
        ApplyReceivedStates();
    }

    private void ApplyReceivedStates()
    {
        lock (latestStates)
        {
            foreach (
                KeyValuePair<int, PlayerNetworkState> pair
                in latestStates
            )
            {
                PlayerNetworkState state = pair.Value;

                if (state.playerId == NetworkSession.Instance.LocalPlayerId)
                {
                    continue;
                }

                if (!players.ContainsKey(state.playerId))
                    continue;

                GameObject player = players[state.playerId];

                if (player == null)
                    continue;

                player.transform.position =
                    new Vector3(
                        state.posX,
                        state.posY,
                        state.posZ
                    );

                player.transform.rotation =
                    Quaternion.Euler(
                        0f,
                        state.rotY,
                        0f
                    );

                Animator animator = player.GetComponentInChildren<Animator>();

                if (animator != null)
                {
                    animator.SetBool(
                        "IsMoving",
                        state.state == 1
                    );
                }
            }
        }
    }

    public void RegisterPlayer(
        int playerId,
        GameObject player)
    {
        if (player == null)
            return;

        players[playerId] = player;
    }

    private void OnDestroy()
    {
        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerStateReceived -=
                ReceivePlayerState;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerStateReceived -=
                ReceivePlayerState;
        }
    }
}