using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkCheeseManager : MonoBehaviour
{
    [Header("Quesos")]
    [SerializeField] private CheeseSpawner cheeseSpawner;

    private LobbyServer lobbyServer;
    private LobbyClient lobbyClient;

    private bool isHost;

    private string pendingCheeseJson;

    private readonly Queue<SpeedEffectData> pendingSpeedEffects = new Queue<SpeedEffectData>();
    private readonly object speedEffectLock = new object();

    private readonly Queue<FreezeEffectData> pendingFreezeEffects = new Queue<FreezeEffectData>();
    private readonly object freezeEffectLock = new object();

    private const float freezeDuration = 5f;
    private Coroutine freezeCoroutine;

    private const float speedDuration = 5f;
    private const float speedMultiplier = 1.5f;


    private readonly Dictionary<int, Coroutine> speedEffectCoroutines = new Dictionary<int, Coroutine>();
    private readonly Dictionary<int, int> speedEffectVersions = new Dictionary<int, int>();

    // =========================================================
    // SOLICITUDES DE RECOLECCIÓN
    // =========================================================

    private readonly Queue<CheeseCollectRequestData>
        pendingCollectRequests = new Queue<CheeseCollectRequestData>();

    private readonly object collectQueueLock = new object();

    // =========================================================
    // QUESOS ACTIVOS DEL HOST
    // =========================================================

    private readonly Dictionary<int, NetworkCheeseData> activeCheeses = new Dictionary<int, NetworkCheeseData>();

    // =========================================================
    // INICIO
    // =========================================================

    private void Start()
    {
        lobbyServer = FindFirstObjectByType<LobbyServer>();

        lobbyClient = FindFirstObjectByType<LobbyClient>();

        lobbyClient.OnFreezeEffectReceived += ReceiveFreezeEffect;

        if (cheeseSpawner == null)
        {
            cheeseSpawner = FindFirstObjectByType<CheeseSpawner>();
        }

        // =====================================================
        // HOST
        // =====================================================

        if (lobbyServer != null &&
            lobbyServer.IsRunning)
        {
            isHost = true;

            Debug.Log(
                "NETWORK CHEESE: SOMOS HOST."
            );

            lobbyServer.OnCheeseCollectRequestReceived += ReceiveCollectRequestFromServer;

            StartCoroutine(
                InitializeHost()
            );

            return;
        }

        // =====================================================
        // CLIENTE
        // =====================================================

        if (lobbyClient != null &&
            lobbyClient.IsConnected)
        {
            isHost = false;

            Debug.Log(
                "NETWORK CHEESE: SOMOS CLIENTE."
            );

            lobbyClient.OnCheeseStateReceived += ReceiveCheeseState;

            lobbyClient.OnCheeseRemoved += ReceiveCheeseRemoved;

            lobbyClient.OnCheeseSpawned += ReceiveCheeseSpawned;

            lobbyClient.OnSpeedEffectReceived += ReceiveSpeedEffect;

            string latestJson = lobbyClient.LatestCheeseStateJson;

            if (!string.IsNullOrEmpty(latestJson))
            {
                pendingCheeseJson = latestJson;
            }

            return;
        }

        Debug.LogWarning(
            "NetworkCheeseManager: no se encontró ni HOST ni CLIENTE conectado."
        );
    }

    // =========================================================
    // INICIALIZAR HOST
    // =========================================================

    private IEnumerator InitializeHost()
    {
        yield return null;
        yield return new WaitForSeconds(0.25f);

        List<NetworkCheeseData> cheeses = cheeseSpawner.SpawnHostCheeses();

        activeCheeses.Clear();

        foreach (
            NetworkCheeseData cheese
            in cheeses)
        {
            activeCheeses[
                cheese.cheeseId
            ] = cheese;
        }

        NetworkCheeseList packet = new NetworkCheeseList
            {
                cheeses = cheeses
            };

        string json = JsonUtility.ToJson(packet);

        Debug.Log(
            "NETWORK CHEESE HOST: enviando "
            + cheeses.Count
            + " quesos."
        );

        if (lobbyServer != null &&
            lobbyServer.IsRunning)
        {
            lobbyServer.BroadcastCheeseState(
                json
            );
        }
    }

    // =========================================================
    // RECIBIR ESTADO INICIAL
    // =========================================================

    private void ReceiveCheeseState(string json)
    {
        pendingCheeseJson = json;
    }

    // =========================================================
    // RECIBIR SOLICITUD DESDE SERVIDOR
    // =========================================================

    private void ReceiveCollectRequestFromServer(int playerId, int cheeseId)
    {
        CheeseCollectRequestData request = new CheeseCollectRequestData
            {
                playerId = playerId,
                cheeseId = cheeseId
            };

        lock (collectQueueLock)
        {
            pendingCollectRequests.Enqueue(
                request
            );
        }
    }

    // =========================================================
    // SOLICITUD DESDE CHEESEPICKUP
    // =========================================================

    public void RequestCheeseCollection(int cheeseId, int playerId)
    {
        // =====================================================
        // HOST
        // =====================================================

        if (isHost)
        {
            Debug.Log(
                "HOST: solicitud local para recoger queso ID "
                + cheeseId
            );

            ProcessCheeseCollection(
                playerId,
                cheeseId
            );

            return;
        }

        // =====================================================
        // CLIENTE
        // =====================================================

        if (lobbyClient == null ||
            !lobbyClient.IsConnected)
        {
            Debug.LogWarning(
                "NetworkCheeseManager: cliente no conectado."
            );

            return;
        }

        lobbyClient.SendCheeseCollectRequest(
            cheeseId,
            playerId
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Estado inicial recibido
        if (!string.IsNullOrEmpty(
            pendingCheeseJson))
        {
            string json = pendingCheeseJson;

            pendingCheeseJson = null;

            ApplyCheeseState(json);
        }

        // Solicitudes de recolección
        if (isHost)
        {
            ProcessPendingCollectRequests();
        }

        ProcessPendingSpeedEffects();

        ProcessPendingFreezeEffects(); 
    }

    // =========================================================
    // PROCESAR COLAS DE RECOLECCIÓN
    // =========================================================

    private void ProcessPendingCollectRequests()
    {
        while (true)
        {
            CheeseCollectRequestData request = null;

            lock (collectQueueLock)
            {
                if (pendingCollectRequests.Count > 0)
                {
                    request = pendingCollectRequests.Dequeue();
                }
            }

            if (request == null)
                break;

            ProcessCheeseCollection(
                request.playerId,
                request.cheeseId
            );
        }
    }

    // =========================================================
    // VALIDAR Y RECOGER
    // =========================================================

    private void ProcessCheeseCollection(int playerId, int cheeseId)
    {
        if (!isHost)
            return;

        // =====================================================
        // ¿EXISTE EL QUESO?
        // =====================================================

        if (!activeCheeses.ContainsKey(cheeseId))
        {
            Debug.LogWarning(
                "HOST: intento de recoger queso inexistente. ID = "
                + cheeseId
            );

            return;
        }

        NetworkCheeseData cheeseData = activeCheeses[cheeseId];

        Debug.Log(
            "HOST: queso válido recogido. ID = "
            + cheeseId
            + " | Tipo = "
            + cheeseData.cheeseType
        );


        if (cheeseData.cheeseType == "Speed")
        {
            SpeedEffectData effect = new SpeedEffectData
            {
                playerId = playerId,
                active = true
            };

            string effectJson = JsonUtility.ToJson(effect);

            // Avisar a los demás jugadores.
            lobbyServer.BroadcastSpeedEffect(effectJson);

            // Aplicar también el efecto en el anfitrión.
            ProcessSpeedEffectLocally(effect);
        }

        if (cheeseData.cheeseType == "Ice")
        {
            NetworkSession session = NetworkSession.Instance;

            if (session == null)
            {
                Debug.LogWarning(
                    "No se encontró NetworkSession para activar el hielo."
                );
            }
            else
            {
                NetworkSession.PlayerData activator =
                    session.GetPlayer(playerId);

                List<int> frozenIds = new List<int>();

                foreach (NetworkSession.PlayerData player in session.Players)
                {
                    if (player.playerId != playerId)
                    {
                        frozenIds.Add(player.playerId);
                    }
                }

                FreezeEffectData effect = new FreezeEffectData
                {
                    activatorPlayerId = playerId,
                    activatorPlayerName = activator != null
                        ? activator.playerName
                        : "Un jugador",
                    frozenPlayerIds = frozenIds.ToArray(),
                    active = true
                };

                string effectJson = JsonUtility.ToJson(effect);

                lobbyServer.BroadcastFreezeEffect(effectJson);

                // El host también procesa el efecto en su propia partida.
                ReceiveFreezeEffect(effectJson);

                Debug.Log(
                    "HOST: queso de hielo activado. Jugador activador: "
                    + playerId
                    + " | Oponentes congelados: "
                    + frozenIds.Count
                );
            }
        }

        if (speedEffectCoroutines.TryGetValue(playerId, out Coroutine previousCoroutine))
        {
            StopCoroutine(previousCoroutine);
        }

        int version = 1;

        if (speedEffectVersions.TryGetValue(playerId, out int previousVersion))
        {
            version = previousVersion + 1;
        }

        speedEffectVersions[playerId] = version;

        speedEffectCoroutines[playerId] = StartCoroutine(EndSpeedEffectAfterDuration(playerId, version));

        lobbyServer.AddPlayerScore(playerId, 1);

        // =====================================================
        // ELIMINAR DEL ESTADO AUTORITATIVO
        // =====================================================

        activeCheeses.Remove(cheeseId);

        // =====================================================
        // ELIMINAR LOCALMENTE
        // =====================================================

        cheeseSpawner.RemoveCheeseById(cheeseId);

        // =====================================================
        // AVISAR A CLIENTES
        // =====================================================

        BroadcastCheeseRemoved(cheeseId);

        // =====================================================
        // CREAR REEMPLAZO DEL MISMO TIPO
        // =====================================================

        NetworkCheeseData replacement =
            cheeseSpawner.SpawnReplacementCheese(
                cheeseId,
                cheeseData.cheeseType
            );

        if (replacement == null)
        {
            Debug.LogError(
                "HOST: no se pudo crear reemplazo para el queso ID "
                + cheeseId
            );

            return;
        }

        activeCheeses[
            replacement.cheeseId
        ] = replacement;

        // =====================================================
        // AVISAR NUEVO QUESO
        // =====================================================

        string replacementJson =
            JsonUtility.ToJson(
                replacement
            );

        BroadcastCheeseSpawned(
            replacementJson
        );

        Debug.Log(
            "HOST: reemplazo creado. ID = "
            + replacement.cheeseId
            + " | Tipo = "
            + replacement.cheeseType
        );
    }

    // =========================================================
    // APLICAR ESTADO INICIAL
    // =========================================================

    private void ApplyCheeseState(string json)
    {

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning(
                "NetworkCheeseManager: JSON vacío."
            );

            return;
        }

        NetworkCheeseList packet;

        try
        {
            packet =
                JsonUtility.FromJson<NetworkCheeseList>(
                    json
                );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "NetworkCheeseManager: error leyendo quesos: "
                + e.Message
            );

            return;
        }

        if (packet == null ||
            packet.cheeses == null)
        {
            Debug.LogError(
                "NetworkCheeseManager: datos inválidos."
            );

            return;
        }

        Debug.Log(
            "CLIENTE: recibiendo "
            + packet.cheeses.Count
            + " quesos."
        );

        cheeseSpawner.ApplyNetworkCheeses(
            packet.cheeses
        );

        Debug.Log(
            "CLIENTE: aplicación terminada. "
            + packet.cheeses.Count
            + " quesos creados."
        );

    }

    // =========================================================
    // QUESO ELIMINADO
    // =========================================================

    private void ReceiveCheeseRemoved(int cheeseId)
    {
        if (isHost)
            return;

        Debug.Log(
            "CLIENTE: eliminando queso ID "
            + cheeseId
        );

        cheeseSpawner.RemoveCheeseById(
            cheeseId
        );
    }

    // =========================================================
    // QUESO REEMPLAZADO
    // =========================================================

    private void ReceiveCheeseSpawned(string json)
    {
        if (isHost)
            return;

        if (string.IsNullOrEmpty(json))
            return;

        NetworkCheeseData cheeseData;

        try
        {
            cheeseData =
                JsonUtility.FromJson<NetworkCheeseData>(
                    json
                );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "NetworkCheeseManager: error leyendo reemplazo: "
                + e.Message
            );

            return;
        }

        if (cheeseData == null)
            return;

        Debug.Log(
            "CLIENTE: creando reemplazo ID "
            + cheeseData.cheeseId
            + " tipo "
            + cheeseData.cheeseType
        );

        cheeseSpawner.ApplySingleNetworkCheese(cheeseData);
    }

    // =========================================================
    // ENVIAR ELIMINACIÓN
    // =========================================================

    private void BroadcastCheeseRemoved(int cheeseId)
    {
        if (lobbyServer == null ||
            !lobbyServer.IsRunning)
        {
            return;
        }

        lobbyServer.BroadcastCheeseRemoved(
            cheeseId
        );
    }

    // =========================================================
    // ENVIAR REEMPLAZO
    // =========================================================

    private void BroadcastCheeseSpawned(string json)
    {
        if (lobbyServer == null ||
            !lobbyServer.IsRunning)
        {
            return;
        }

        lobbyServer.BroadcastCheeseSpawned(
            json
        );
    }

    // =========================================================
    // DESTRUIR
    // =========================================================

    private void OnDestroy()
    {
        if (lobbyServer != null)
        {
            lobbyServer.OnCheeseCollectRequestReceived -= ReceiveCollectRequestFromServer;
        }


        if (lobbyClient != null)
        {
            lobbyClient.OnCheeseStateReceived -= ReceiveCheeseState;

            lobbyClient.OnCheeseRemoved -= ReceiveCheeseRemoved;

            lobbyClient.OnCheeseSpawned -= ReceiveCheeseSpawned;

            lobbyClient.OnSpeedEffectReceived -= ReceiveSpeedEffect;

            lobbyClient.OnFreezeEffectReceived -= ReceiveFreezeEffect;
        }

    }

    private void ReceiveSpeedEffect(string json)
    {
        SpeedEffectData effect = JsonUtility.FromJson<SpeedEffectData>(json);

        if (effect == null)
        {
            Debug.LogWarning("No se pudo leer el efecto de velocidad.");
            return;
        }

        lock (speedEffectLock)
        {
            pendingSpeedEffects.Enqueue(effect);
        }
    }


    private void ReceiveFreezeEffect(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        FreezeEffectData effect;

        try
        {
            effect = JsonUtility.FromJson<FreezeEffectData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "No se pudo leer el efecto de hielo: " + e.Message
            );
            return;
        }

        if (effect == null)
            return;

        lock (freezeEffectLock)
        {
            pendingFreezeEffects.Enqueue(effect);
        }
    }


    private void ProcessPendingSpeedEffects()
    {
        while (true)
        {
            SpeedEffectData effect;

            lock (speedEffectLock)
            {
                if (pendingSpeedEffects.Count == 0)
                    return;

                effect = pendingSpeedEffects.Dequeue();
            }

            Debug.Log(
                "Efecto de velocidad recibido para el jugador ID: "
                + effect.playerId
                + " | Activo: "
                + effect.active
            );

            if (effect.playerId != NetworkSession.Instance.LocalPlayerId)
            {
                continue;
            }

            NetworkPlayerReceiver receiver = FindFirstObjectByType<NetworkPlayerReceiver>();

            GameObject player = receiver.GetPlayer(effect.playerId);

            if (player == null)
            {
                Debug.LogWarning(
                    "No se encontró el jugador con ID: " + effect.playerId
                );
                continue;
            }

            ProcessSpeedEffectLocally(effect);
        }
    }

    private void ProcessSpeedEffectLocally(SpeedEffectData effect)
    {
        if (effect.playerId != NetworkSession.Instance.LocalPlayerId)
            return;

        NetworkPlayerReceiver receiver = FindFirstObjectByType<NetworkPlayerReceiver>();

        if (receiver == null)
        {
            Debug.LogWarning("No se encontró NetworkPlayerReceiver.");
            return;
        }

        GameObject player = receiver.GetPlayer(effect.playerId);

        if (player == null)
        {
            Debug.LogWarning(
                "No se encontró el jugador con ID: " + effect.playerId
            );
            return;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            Debug.LogWarning("El jugador no tiene PlayerMovement.");
            return;
        }

        if (effect.active)
        {
            movement.ActivarVelocidad(speedMultiplier, speedDuration);

            GameHUD hud = FindFirstObjectByType<GameHUD>();

            if (hud != null)
            {
                hud.MostrarSupervelocidad(effect.playerId);
            }
        }

        else
        {
            movement.DesactivarVelocidad();

            GameHUD hud = FindFirstObjectByType<GameHUD>();

            if (hud != null)
            {
                hud.OcultarSupervelocidad();
            }
        }
    }

    private void ProcessPendingFreezeEffects()
    {
        while (true)
        {
            FreezeEffectData effect;

            lock (freezeEffectLock)
            {
                if (pendingFreezeEffects.Count == 0)
                    return;

                effect = pendingFreezeEffects.Dequeue();
            }

            Debug.Log(
                "Efecto de hielo recibido. Activador: "
                + effect.activatorPlayerId
                + " | Activo: "
                + effect.active
            );

            if (effect.frozenPlayerIds == null)
                continue;

            if (NetworkSession.Instance == null)
                continue;

            NetworkPlayerReceiver receiver = FindFirstObjectByType<NetworkPlayerReceiver>();

            if (receiver != null && effect.active)
            {
                receiver.AplicarCongelamiento(
                    effect.frozenPlayerIds,
                    freezeDuration
                );
            }

            GameHUD hud = FindFirstObjectByType<GameHUD>();

            if (hud != null && effect.active)
            {
                hud.ActualizarJugadoresCongelados(
                    effect.frozenPlayerIds
                );
            }

            int localPlayerId = NetworkSession.Instance.LocalPlayerId;

            bool localPlayerIsFrozen = false;

            foreach (int frozenId in effect.frozenPlayerIds)
            {
                if (frozenId == localPlayerId)
                {
                    localPlayerIsFrozen = true;
                    break;
                }
            }

            FreezeEffectUI freezeUI = FindFirstObjectByType<FreezeEffectUI>();

            if (freezeUI != null &&
                effect.activatorPlayerId == localPlayerId)
            {
                freezeUI.MostrarMensaje(
                    "Has congelado a tus oponentes",
                    2f
                );
            }

            if (!localPlayerIsFrozen)
                continue;

            if (receiver == null)
            {
                Debug.LogWarning(
                    "No se encontró NetworkPlayerReceiver."
                );
                continue;
            }

            GameObject localPlayer = receiver.GetPlayer(localPlayerId);

            if (localPlayer == null)
            {
                Debug.LogWarning(
                    "No se encontró el jugador local."
                );
                continue;
            }

            PlayerMovement movement = localPlayer.GetComponent<PlayerMovement>();

            if (movement == null)
            {
                Debug.LogWarning(
                    "El jugador no tiene PlayerMovement."
                );
                continue;
            }

            if (effect.active)
            {
                movement.CongelarMovimiento();

                if (freezeUI != null)
                {
                    freezeUI.IniciarSecuenciaCongelado(
                        effect.activatorPlayerName
                    );
                }

                if (freezeCoroutine != null)
                    StopCoroutine(freezeCoroutine);

                freezeCoroutine = StartCoroutine(
                    DescongelarDespuesDeCincoSegundos(movement)
                );
            }
            else
            {
                movement.DescongelarMovimiento();

                if (freezeCoroutine != null)
                {
                    StopCoroutine(freezeCoroutine);
                    freezeCoroutine = null;
                }
            }
        }
    }

    private System.Collections.IEnumerator EndSpeedEffectAfterDuration(
        int playerId,
        int version)
    {
        yield return new WaitForSeconds(speedDuration);

        if (!speedEffectVersions.TryGetValue(playerId, out int currentVersion)
            || currentVersion != version)
        {
            yield break;
        }

        speedEffectCoroutines.Remove(playerId);
        speedEffectVersions.Remove(playerId);

        SpeedEffectData effect = new SpeedEffectData
        {
            playerId = playerId,
            active = false
        };

        lobbyServer.BroadcastSpeedEffect(JsonUtility.ToJson(effect));
        ProcessSpeedEffectLocally(effect);
    }

    private IEnumerator DescongelarDespuesDeCincoSegundos(
        PlayerMovement movement)
    {
        yield return new WaitForSeconds(freezeDuration);

        if (movement != null)
            movement.DescongelarMovimiento();

        freezeCoroutine = null;

        Debug.Log("El efecto de hielo ha terminado.");
    }
}