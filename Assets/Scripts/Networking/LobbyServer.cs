using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class LobbyServer : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private int port = 7777;

    [Header("Partida")]
    [SerializeField] private int matchDurationSeconds = 120;

    private TcpListener server;
    private Thread serverThread;

    private bool isRunning;

    private int remainingMatchTime;
    private bool matchRunning;
    private Coroutine matchTimerCoroutine;

    private readonly object clientsLock = new object();

    private readonly List<ClientConnection> clients = new List<ClientConnection>();

    private readonly List<LobbyPlayer> players = new List<LobbyPlayer>();

    private readonly Dictionary<int, int> playerScores = new Dictionary<int, int>();

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private int nextPlayerId = 2;

    private string hostName;
    private int hostId = 1;

    public bool IsRunning => isRunning;

    public event Action<string> OnPlayerListChanged;
    public event Action<string> OnCountdownMessage;
    public event Action<string> OnSpawnAssignmentsReady;
    public event Action<string> OnPlayerStateReceived;
    public event Action<int, int> OnCheeseCollectRequestReceived;
    public event Action<string> OnScoreStateReceived;
    public event Action<int> OnMatchTimeReceived;
    public int LatestMatchTime { get; private set; } = -1;
    public event Action OnMatchFinished;
    public event Action OnMatchCancelled;


    private void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }


    // ---------------------------------------------------------
    // MOSTRAR IP
    // ---------------------------------------------------------

    public string LocalIPAddress
    {
        get
        {
            try
            {
                string hostName = Dns.GetHostName();

                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = address.ToString();

                        if (!ip.StartsWith("127."))
                        {
                            return ip;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "No se pudo obtener la IP: " +
                    e.Message
                );
            }

            return "IP no disponible";
        }
    }

    // ---------------------------------------------------------
    // INICIAR SERVIDOR
    // ---------------------------------------------------------

    public void StartServer(string playerName)
    {
        if (isRunning)
        {
            Debug.Log("El servidor ya está iniciado.");
            return;
        }

        hostName = playerName;

        players.Clear();

        // El host siempre es el jugador 1
        players.Add(new LobbyPlayer
        {
            playerId = hostId,
            playerName = hostName
        });

        playerScores.Clear();
        playerScores[hostId] = 0;

        if (NetworkSession.Instance != null)
        {
            NetworkSession.Instance.SetLocalPlayerId(hostId);
        }

        isRunning = true;

        serverThread = new Thread(ServerLoop);
        serverThread.IsBackground = true;
        serverThread.Start();

        Debug.Log("Servidor iniciado.");
        Debug.Log("Puerto: " + port);
    }

    // ---------------------------------------------------------
    // ESPERAR CONEXIONES
    // ---------------------------------------------------------

    private void ServerLoop()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient tcpClient = server.AcceptTcpClient();

                    Debug.Log(
                        "DIAGNOSTICO SERVIDOR: conexión TCP recibida desde " +
                        tcpClient.Client.RemoteEndPoint
                    );

                    lock (clientsLock)
                    {
                        if (clients.Count >= 3)
                        {
                            tcpClient.Close();
                            continue;
                        }

                        ClientConnection connection =
                            new ClientConnection(tcpClient, this);

                        clients.Add(connection);

                        Thread clientThread =
                            new Thread(connection.Listen);

                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                }
                catch (SocketException)
                {
                    if (!isRunning)
                        break;

                }
            }
        }
        catch (Exception e)
        {
            if (isRunning)
            {
                Debug.LogError("Error del servidor: " + e.Message);
            }
        }
    }

    // ---------------------------------------------------------
    // NUEVO JUGADOR
    // ---------------------------------------------------------

    public void RegisterClient(ClientConnection connection, string playerName)
    {
        lock (clientsLock)
        {
            if (players.Count >= 4)
            {
                return;
            }

            int id = nextPlayerId;
            nextPlayerId++;

            LobbyPlayer player = new LobbyPlayer
            {
                playerId = id,
                playerName = playerName
            };

            players.Add(player);
            playerScores[id] = 0;

            connection.PlayerId = id;
            connection.PlayerName = playerName;

            SendMessage(
                connection,
                new NetworkMessage
                {
                    type = "Welcome",
                    playerId = id,
                    playerName = playerName
                }
            );

            BroadcastPlayerList();
        }

        Debug.Log(
            "Jugador conectado: " +
            playerName
        );
    }

    // ---------------------------------------------------------
    // LISTA DE JUGADORES
    // ---------------------------------------------------------

    private void BroadcastPlayerList()
    {
        string playerData = "";

        foreach (LobbyPlayer player in players)
        {
            playerData +=
                player.playerId +
                "|" +
                player.playerName +
                ";";
        }

        OnPlayerListChanged?.Invoke(playerData);

        NetworkMessage message = new NetworkMessage
        {
            type = "PlayerList",
            data = playerData
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        Debug.Log("Lista actualizada: " + playerData);
    }

    // ---------------------------------------------------------
    // ENVIAR MENSAJE
    // ---------------------------------------------------------

    private void SendMessage(ClientConnection client, NetworkMessage message)
    {
        try
        {
            string json = JsonUtility.ToJson(message);

            client.Writer.WriteLine(json);
            client.Writer.Flush();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "No se pudo enviar mensaje: " +
                e.Message
            );
        }
    }

    public void BroadcastPlayerState(NetworkMessage message)
    {

        // Entrega el estado al HOST
        OnPlayerStateReceived?.Invoke(message.data);

        // Entrega el estado a los CLIENTES
        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }
    }

    public void ReceiveCheeseCollectRequest(int playerId, int cheeseId)
    {
        Debug.Log(
            "SERVIDOR: solicitud recibida. Jugador "
            + playerId
            + " queso ID "
            + cheeseId
        );

        OnCheeseCollectRequestReceived?.Invoke(
            playerId,
            cheeseId
        );
    }


    public void BroadcastCheeseState(string cheeseJson)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = "CheeseState",
            data = cheeseJson
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        Debug.Log(
            "Servidor: estado de quesos enviado a todos los clientes."
        );
    }

    public void BroadcastCheeseRemoved(
    int cheeseId)
    {
        NetworkMessage message =  new NetworkMessage
            {
                type = "CheeseRemoved",
                data = cheeseId.ToString()
            };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(
                    client,
                    message
                );
            }
        }

        Debug.Log(
            "SERVIDOR: queso eliminado enviado. ID = "
            + cheeseId
        );
    }

    public void BroadcastCheeseSpawned(
        string cheeseJson)
    {
        NetworkMessage message = new NetworkMessage
            {
                type = "CheeseSpawned",
                data = cheeseJson
            };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(
                    client,
                    message
                );
            }
        }

        Debug.Log(
            "SERVIDOR: reemplazo de queso enviado."
        );

    }

    public void BroadcastSpeedEffect(string effectJson)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = "SpeedEffect",
            data = effectJson
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        Debug.Log("SERVIDOR: efecto de velocidad enviado a los clientes.");
    }

    public void BroadcastFreezeEffect(string effectJson)
    {
        NetworkMessage message = new NetworkMessage
        {
            type = "FreezeEffect",
            data = effectJson
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        Debug.Log("SERVIDOR: efecto de congelamiento enviado a los clientes.");
    }


    public void AddPlayerScore(int playerId, int amount)
    {
        lock (clientsLock)
        {
            if (!playerScores.ContainsKey(playerId))
                return;

            playerScores[playerId] += amount;
        }

        Debug.Log(
            "PUNTAJE: jugador "
            + playerId
            + " ahora tiene "
            + playerScores[playerId]
            + " puntos."
        );

        BroadcastScoreState();
    }

    private void BroadcastScoreState()
    {
        PlayerScoreList packet = new PlayerScoreList();

        lock (clientsLock)
        {
            foreach (var entry in playerScores)
            {
                LobbyPlayer jugador = players.Find(
                    p => p.playerId == entry.Key
                );

                string playerName = jugador != null
                    ? jugador.playerName
                    : "Jugador " + entry.Key;

                packet.players.Add(new PlayerScoreData
                {
                    playerId = entry.Key,
                    playerName = playerName,
                    score = entry.Value
                });
            }
        }

        string json = JsonUtility.ToJson(packet);

        NetworkMessage message = new NetworkMessage
        {
            type = "ScoreState",
            data = json
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        OnScoreStateReceived?.Invoke(json);
    }

    public void BroadcastScoreStateForHUD()
    {
        BroadcastScoreState();
    }


    // ---------------------------------------------------------
    // TEMPORIZADOR
    // ---------------------------------------------------------

    public void StartMatchTimer()
    {
        if (!isRunning)
            return;

        if (matchRunning)
            return;

        if (players.Count < 2)
            return;

        if (matchTimerCoroutine != null)
            StopCoroutine(matchTimerCoroutine);

        matchRunning = true;
        matchTimerCoroutine = StartCoroutine(MatchTimerCoroutine());
    }

    private IEnumerator MatchTimerCoroutine()
    {
        matchRunning = true;
        remainingMatchTime = matchDurationSeconds;

        BroadcastMatchTime(remainingMatchTime);

        Debug.Log(
            "PARTIDA INICIADA. Duración: "
            + matchDurationSeconds
            + " segundos."
        );

        while (remainingMatchTime > 0 && matchRunning)
        {
            yield return new WaitForSeconds(1f);

            remainingMatchTime--;

            BroadcastMatchTime(remainingMatchTime);
        }

        if (!matchRunning)
            yield break;

        matchRunning = false;

        Debug.Log("TIEMPO TERMINADO.");

        BroadcastScoreState();
        BroadcastMatchFinished();
    }

    private void BroadcastMatchTime(int seconds)
    {
        LatestMatchTime = seconds;

        NetworkMessage message = new NetworkMessage
        {
            type = "MatchTime",
            data = seconds.ToString()
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        OnMatchTimeReceived?.Invoke(seconds);
    }


    private void BroadcastMatchFinished()
    {
        NetworkMessage message = new NetworkMessage
        {
            type = "MatchFinished",
            data = ""
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        OnMatchFinished?.Invoke();
    }


    // ---------------------------------------------------------
    // DESCONEXIÓN
    // ---------------------------------------------------------

    public void RemoveClient(ClientConnection connection)
    {
        lock (clientsLock)
        {
            clients.Remove(connection);

            LobbyPlayer player =
                players.Find(
                    p => p.playerId == connection.PlayerId
                );

            if (player != null)
            {
                players.Remove(player);
            }

            playerScores.Remove(connection.PlayerId);

            BroadcastPlayerList();
        }

        Debug.Log(
            "Jugador desconectado: " +
            connection.PlayerName
        );

        // Actualizar el ranking de los jugadores que siguen conectados.
        BroadcastScoreState();

        if (matchRunning && players.Count < 2)
        {
            mainThreadActions.Enqueue(
                () => CancelMatchBecauseOfConnection()
            );
        }
    }

    private void CancelMatchBecauseOfConnection()
    {
        if (!matchRunning)
            return;

        matchRunning = false;

        if (matchTimerCoroutine != null)
        {
            StopCoroutine(matchTimerCoroutine);
            matchTimerCoroutine = null;
        }

        NetworkMessage message = new NetworkMessage
        {
            type = "MatchCancelled",
            data = ""
        };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, message);
            }
        }

        OnMatchCancelled?.Invoke();
    }


    // ---------------------------------------------------------
    // DETENER SERVIDOR
    // ---------------------------------------------------------

    public void StopServer()
    {
        isRunning = false;

        try
        {
            server?.Stop();
        }
        catch
        {
        }

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                client.Close();
            }

            clients.Clear();

            players.Clear();
            nextPlayerId = 2;
            hostName = null;
        }

        if (serverThread != null &&
            serverThread.IsAlive)
        {
            serverThread.Join(200);
        }

        Debug.Log("Servidor detenido.");
    }

    private void OnDestroy()
    {
        StopServer();
    }

    // =========================================================
    // CLASE DEL JUGADOR
    // =========================================================

    [Serializable]
    private class LobbyPlayer
    {
        public int playerId;
        public string playerName;
    }

    // =========================================================
    // CONEXIÓN DE CLIENTE
    // =========================================================

    public class ClientConnection
    {
        private TcpClient tcpClient;
        private LobbyServer server;

        private StreamReader reader;

        public StreamWriter Writer { get; private set; }

        public int PlayerId { get; set; }

        public string PlayerName { get; set; }

        public ClientConnection(
            TcpClient client,
            LobbyServer lobbyServer)
        {
            tcpClient = client;
            server = lobbyServer;

            NetworkStream stream =  tcpClient.GetStream();

            reader =
                new StreamReader(
                    stream,
                    Encoding.UTF8
                );

            Writer =
                new StreamWriter(
                    stream,
                    Encoding.UTF8
                )
                {
                    AutoFlush = true
                };
        }

        public void Listen()
        {
            try
            {
                while (tcpClient.Connected)
                {
                    string json = reader.ReadLine();

                    if (string.IsNullOrEmpty(json))
                        break;

                    NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(json);

                    if (message.type == "Hello")
                    {
                        server.RegisterClient(
                            this,
                            message.playerName
                        );
                    }
                    else if (message.type == "PlayerState")
                    {
                        message.playerId = PlayerId;

                        server.BroadcastPlayerState(message);
                    }
                    else if (message.type == "CheeseCollectRequest")
                    {
                        CheeseCollectRequestData request =
                            JsonUtility.FromJson<CheeseCollectRequestData>(
                                message.data
                            );

                        if (request != null)
                        {
                            // El servidor decide quién es realmente
                            int realPlayerId = PlayerId;

                            server.ReceiveCheeseCollectRequest(
                                realPlayerId,
                                request.cheeseId
                            );
                        }
                    }
                }
            }
            catch
            {
                // La conexión terminó.
            }

            server.RemoveClient(this);
            Close();
        }

        public void Close()
        {
            try
            {
                reader?.Close();
                Writer?.Close();
                tcpClient?.Close();
            }
            catch
            {
            }
        }
    }

    public void StartCountdown()
    {
        int cantidadJugadores;

        lock (clientsLock)
        {
            cantidadJugadores = players.Count;
        }


        CrearAsignacionDeSpawns();

        StartCoroutine(CuentaRegresivaCoroutine());
    }

    private IEnumerator CuentaRegresivaCoroutine()
    {
        EnviarCuentaRegresiva("PREPARADOS");

        yield return new WaitForSeconds(1f);

        EnviarCuentaRegresiva("3");

        yield return new WaitForSeconds(1f);

        EnviarCuentaRegresiva("2");

        yield return new WaitForSeconds(1f);

        EnviarCuentaRegresiva("1");

        yield return new WaitForSeconds(1f);

        EnviarCuentaRegresiva("¡A JUGAR!");

        yield return new WaitForSeconds(1.2f);

        StartMatchTimer();

        BroadcastScoreState();
    }

    private void EnviarCuentaRegresiva(string mensaje)
    {
        NetworkMessage networkMessage = new NetworkMessage
        {
            type = "Countdown",
            data = mensaje
        };

        // Actualiza la pantalla del HOST
        OnCountdownMessage?.Invoke(mensaje);

        // Envía el mensaje a los CLIENTES
        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(client, networkMessage);
            }
        }

    }

    private void CrearAsignacionDeSpawns()
    {
        if (players.Count < 2 || players.Count > 4)
        {
            Debug.LogWarning(
                "No se puede asignar spawn. " +
                "La partida necesita entre 2 y 4 jugadores."
            );

            return;
        }

        List<int> spawnIndexes = Enumerable.Range(0, 4).ToList();

        for (int i = spawnIndexes.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            int temporal = spawnIndexes[i];

            spawnIndexes[i] = spawnIndexes[randomIndex];

            spawnIndexes[randomIndex] = temporal;
        }

        NetworkSession session = NetworkSession.Instance;

        if (session == null)
        {
            Debug.LogError(
                "No se encontró NetworkSession."
            );

            return;
        }

        session.ClearPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            LobbyPlayer player = players[i];

            session.AddPlayer(
                player.playerId,
                player.playerName,
                spawnIndexes[i]
            );
        }

        string json = session.GetPlayersJson();

        Debug.Log(
            "Asignación de spawns creada:"
        );

        Debug.Log(json);

        OnSpawnAssignmentsReady?.Invoke(json);

        NetworkMessage message = new NetworkMessage
            {
                type = "SpawnAssignments",
                data = json
            };

        lock (clientsLock)
        {
            foreach (ClientConnection client in clients)
            {
                SendMessage(
                    client,
                    message
                );
            }
        }
    }
}
