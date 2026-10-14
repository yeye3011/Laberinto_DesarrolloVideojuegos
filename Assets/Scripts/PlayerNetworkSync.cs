using System.Collections;
using UnityEngine;

public class PlayerNetworkSync : MonoBehaviour
{
    [Header("Sincronización")]
    [SerializeField] private float sendInterval = 0.05f;

    private LobbyClient lobbyClient;
    private LobbyServer lobbyServer;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        lobbyClient =
            FindFirstObjectByType<LobbyClient>();

        lobbyServer =
            FindFirstObjectByType<LobbyServer>();

        StartCoroutine(EnviarEstadoPeriodicamente());
    }

    private IEnumerator EnviarEstadoPeriodicamente()
    {
        WaitForSeconds wait =
            new WaitForSeconds(sendInterval);

        while (true)
        {
            EnviarEstado();

            yield return wait;
        }
    }

    private void EnviarEstado()
    {
        if (NetworkSession.Instance == null)
            return;

        if (playerMovement == null)
            return;

        if (!playerMovement.IsLocalPlayer)
            return;

        PlayerNetworkState state =
            new PlayerNetworkState
            {
                playerId =
                    NetworkSession.Instance.LocalPlayerId,

                posX = transform.position.x,
                posY = transform.position.y,
                posZ = transform.position.z,

                rotY = transform.eulerAngles.y,

                state = playerMovement.CurrentState
            };

        // ============================
        // CLIENTE
        // ============================

        if (lobbyClient != null &&
            lobbyClient.IsConnected)
        {

            lobbyClient.SendPlayerState(state);

            return;
        }

        // ============================
        // HOST
        // ============================

        if (lobbyServer != null)
        {
            NetworkMessage message =
                new NetworkMessage
                {
                    type = "PlayerState",
                    playerId = state.playerId,
                    data = JsonUtility.ToJson(state)
                };

            lobbyServer.BroadcastPlayerState(message);
        }
    }
}