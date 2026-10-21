using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerReceiver : MonoBehaviour
{
    private LobbyClient lobbyClient;
    private LobbyServer lobbyServer;

    private readonly Dictionary<int, PlayerNetworkState> latestStates = new Dictionary<int, PlayerNetworkState>();

    private readonly Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();


    private readonly HashSet<int> frozenPlayers = new HashSet<int>();

    private readonly Dictionary<int, float> pendingFreezes = new Dictionary<int, float>();

    private readonly Dictionary<int, Coroutine> freezeCoroutines = new Dictionary<int, Coroutine>();

    private readonly Dictionary<int, Renderer[]> playerRenderers = new Dictionary<int, Renderer[]>();

    private readonly Dictionary<int, MaterialPropertyBlock[]> originalBlocks = new Dictionary<int, MaterialPropertyBlock[]>();

    private static readonly Color frozenBlue = new Color32(118, 155, 177, 255);

    private void Start()
    {
        lobbyClient = FindFirstObjectByType<LobbyClient>();

        lobbyServer = FindFirstObjectByType<LobbyServer>();

        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerStateReceived += ReceivePlayerState;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerStateReceived += ReceivePlayerState;
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

                if (NetworkSession.Instance == null)
                {
                    continue;
                }

                if (state.playerId == NetworkSession.Instance.LocalPlayerId)
                {
                    continue;
                }

                if (!players.ContainsKey(state.playerId))
                    continue;

                GameObject player = players[state.playerId];

                if (player == null)
                    continue;

                if (frozenPlayers.Contains(state.playerId))
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

    public void AplicarCongelamiento(int[] playerIds, float duration)
    {
        if (playerIds == null)
            return;

        foreach (int playerId in playerIds)
        {
            if (!players.TryGetValue(playerId, out GameObject player) || player == null)
            {
                pendingFreezes[playerId] = Time.time + duration;

                Debug.Log(
                    "Congelamiento pendiente para el jugador: " + playerId
                );

                continue;
            }

            pendingFreezes.Remove(playerId);

            if (!playerRenderers.ContainsKey(playerId))
            {
                Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);

                MaterialPropertyBlock[] blocks = new MaterialPropertyBlock[renderers.Length];

                for (int i = 0; i < renderers.Length; i++)
                {
                    blocks[i] = new MaterialPropertyBlock();
                    renderers[i].GetPropertyBlock(blocks[i]);
                }

                playerRenderers[playerId] = renderers;
                originalBlocks[playerId] = blocks;
            }

            frozenPlayers.Add(playerId);

            Renderer[] playerParts = playerRenderers[playerId];

            foreach (Renderer part in playerParts)
            {
                if (part == null)
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                part.GetPropertyBlock(block);

                foreach (Material material in part.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    if (material.HasProperty("_BaseColor"))
                        block.SetColor("_BaseColor", frozenBlue);

                    if (material.HasProperty("_Color"))
                        block.SetColor("_Color", frozenBlue);
                }

                part.SetPropertyBlock(block);
            }

            if (freezeCoroutines.TryGetValue(
                playerId, out Coroutine runningCoroutine))
            {
                if (runningCoroutine != null)
                    StopCoroutine(runningCoroutine);
            }

            freezeCoroutines[playerId] = StartCoroutine(RestaurarJugadorCongelado(playerId, duration));
        }
    }

    private System.Collections.IEnumerator RestaurarJugadorCongelado(int playerId, float duration)
    {
        yield return new WaitForSeconds(duration);

        frozenPlayers.Remove(playerId);

        GameHUD hud = FindFirstObjectByType<GameHUD>();

        if (hud != null)
        {
            hud.QuitarJugadorCongelado(playerId);
        }

        if (playerRenderers.TryGetValue(
            playerId, out Renderer[] renderers) &&
            originalBlocks.TryGetValue(
            playerId, out MaterialPropertyBlock[] blocks))
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].SetPropertyBlock(blocks[i]);
            }
        }

        freezeCoroutines.Remove(playerId);

        Debug.Log(
            "Jugador descongelado visualmente: " + playerId
        );
    }

    public void RegisterPlayer(int playerId, GameObject player)
    {
        if (player == null) return;

        players[playerId] = player;

        if (pendingFreezes.TryGetValue(playerId, out float expirationTime))
        {
            pendingFreezes.Remove(playerId);

            float remainingDuration = expirationTime - Time.time;

            if (remainingDuration > 0f)
            {
                AplicarCongelamiento(
                    new int[] { playerId },
                    remainingDuration
                );
            }
        }
    }

    public GameObject GetPlayer(int playerId)
    {
        if (players.TryGetValue(playerId, out GameObject player))
        {
            return player;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerStateReceived -= ReceivePlayerState;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerStateReceived -= ReceivePlayerState;
        }
    }
}