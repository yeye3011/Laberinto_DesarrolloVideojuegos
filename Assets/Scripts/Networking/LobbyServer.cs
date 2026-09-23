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

public class LobbyServer : MonoBehaviour
{
    [Header("Servidor")]
    [SerializeField] private int port = 7777;

    private TcpListener server;
    private Thread serverThread;

    private bool isRunning;

    private readonly object clientsLock = new object();

    private readonly List<ClientConnection> clients = new List<ClientConnection>();

    private readonly List<LobbyPlayer> players = new List<LobbyPlayer>();

    private int nextPlayerId = 2;

    private string hostName;
    private int hostId = 1;

    public bool IsRunning => isRunning;

    public event Action<string> OnPlayerListChanged;
    public event Action<string> OnCountdownMessage;
    public event Action<string> OnSpawnAssignmentsReady;
    public event Action<string> OnPlayerStateReceived;

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

            Debug.Log("Servidor escuchando en el puerto " + port);

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

                    Debug.LogError("Error aceptando conexión.");
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
        Debug.Log("PlayerState recibido: " + message.data);

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

            BroadcastPlayerList();
        }

        Debug.Log(
            "Jugador desconectado: " +
            connection.PlayerName
        );
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

            NetworkStream stream =
                tcpClient.GetStream();

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

        if (cantidadJugadores < 2 || cantidadJugadores > 4)
        {
            Debug.Log("No se puede iniciar. Se necesitan entre 2 y 4 jugadores.");
            return;
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

        yield return new WaitForSeconds(1f);
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

        Debug.Log("Cuenta regresiva: " + mensaje);
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

        List<int> spawnIndexes =
            Enumerable.Range(0, 4).ToList();

        for (int i = spawnIndexes.Count - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            int temporal =
                spawnIndexes[i];

            spawnIndexes[i] =
                spawnIndexes[randomIndex];

            spawnIndexes[randomIndex] =
                temporal;
        }

        NetworkSession session =
            NetworkSession.Instance;

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
            LobbyPlayer player =
                players[i];

            session.AddPlayer(
                player.playerId,
                player.playerName,
                spawnIndexes[i]
            );
        }

        string json =
            session.GetPlayersJson();

        Debug.Log(
            "Asignación de spawns creada:"
        );

        Debug.Log(json);

        OnSpawnAssignmentsReady?.Invoke(json);

        NetworkMessage message =
            new NetworkMessage
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
