using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LobbyClient : MonoBehaviour
{
    [Header("Conexión")]
    [SerializeField] private int port = 7777;

    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private const int connectionTimeoutMilliseconds = 10000;

    private Thread receiveThread;

    private bool isConnected;

    public bool IsConnected => isConnected;

    public event Action<string> OnPlayerListReceived;
    public event Action OnConnectionFailed;
    public event Action OnConnectionSucceeded;
    public event Action<string> OnCountdownMessageReceived;
    public event Action<string> OnSpawnAssignmentsReceived;
    public event Action<string> OnPlayerStateReceived;

    // ---------------------------------------------------------
    // CONECTAR
    // ---------------------------------------------------------

    public void ConnectToServer(string hostIP, string playerName)
    {
        if (isConnected)
        {
            Debug.Log("Ya estás conectado.");
            return;
        }

        receiveThread = new Thread(
            () => ConnectThread(hostIP, playerName)
        );

        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ConnectThread(string hostIP, string playerName)
    {
        try
        {
            Debug.Log("DIAGNOSTICO: intentando conectar a " + hostIP + ":" + port);

            client = new TcpClient();

            Debug.Log("DIAGNOSTICO: TcpClient creado.");

            IAsyncResult connectionResult =
                client.BeginConnect(hostIP, port, null, null);

            Debug.Log("DIAGNOSTICO: BeginConnect ejecutado.");

            bool connectionCompleted =
                connectionResult.AsyncWaitHandle.WaitOne(
                    connectionTimeoutMilliseconds
                );

            if (!connectionCompleted)
            {
                Debug.LogError(
                    "DIAGNOSTICO: TIMEOUT conectando a " +
                    hostIP + ":" + port
                );

                client.Close();
                isConnected = false;
                OnConnectionFailed?.Invoke();
                return;
            }

            client.EndConnect(connectionResult);
            Debug.Log("DIAGNOSTICO: conexión TCP establecida.");

            NetworkStream stream = client.GetStream();

            reader =
                new StreamReader(
                    stream,
                    Encoding.UTF8
                );

            writer =
                new StreamWriter(
                    stream,
                    Encoding.UTF8
                )
                {
                    AutoFlush = true
                };

            isConnected = true;

            Debug.Log("¡Conectado al servidor!");

            // Avisar al servidor quién somos
            NetworkMessage hello =
                new NetworkMessage
                {
                    type = "Hello",
                    playerName = playerName
                };

            SendMessage(hello);

            // Empezar a escuchar al servidor
            Listen();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "No se pudo conectar: " +
                e.Message
            );

            isConnected = false;

            OnConnectionFailed?.Invoke();
        }
    }

    // ---------------------------------------------------------
    // ESCUCHAR SERVIDOR
    // ---------------------------------------------------------

    private void Listen()
    {
        try
        {
            while (isConnected &&
                   client != null &&
                   client.Connected)
            {
                string json =
                    reader.ReadLine();

                if (string.IsNullOrEmpty(json))
                    break;

                NetworkMessage message =
                    JsonUtility.FromJson<NetworkMessage>(
                        json
                    );

                ProcessMessage(message);
            }
        }
        catch (Exception e)
        {
            if (isConnected)
            {
                Debug.LogWarning(
                    "Conexión perdida: " +
                    e.Message
                );
            }
        }

        isConnected = false;
    }

    // ---------------------------------------------------------
    // PROCESAR MENSAJES
    // ---------------------------------------------------------

    private void ProcessMessage(NetworkMessage message)
    {
        if (message == null)
            return;

        switch (message.type)
        {
            case "Welcome":

                Debug.Log(
                    "Bienvenido. Tu ID es: " +
                    message.playerId
                );

                if (NetworkSession.Instance != null)
                {
                    NetworkSession.Instance.SetLocalPlayerId(
                        message.playerId
                    );
                }

                OnConnectionSucceeded?.Invoke();

                break;

            case "PlayerList":
                OnPlayerListReceived?.Invoke(message.data);
                break;

            case "Countdown":
                OnCountdownMessageReceived?.Invoke(message.data);
                break;

            case "SpawnAssignments":

                Debug.Log(
                    "Asignación de spawns recibida."
                );

                OnSpawnAssignmentsReceived?.Invoke(
                    message.data
                );

                break;

            case "PlayerState":
                OnPlayerStateReceived?.Invoke(message.data);
                break;
        }
    }

    // ---------------------------------------------------------
    // ENVIAR MENSAJE
    // ---------------------------------------------------------

    private void SendMessage(
        NetworkMessage message)
    {
        try
        {
            string json =
                JsonUtility.ToJson(message);

            writer.WriteLine(json);
            writer.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Error enviando mensaje: " +
                e.Message
            );
        }
    }

    public void SendPlayerState(PlayerNetworkState state)
    {
        if (!isConnected)
            return;

        if (state == null)
            return;

        NetworkMessage message =
            new NetworkMessage
            {
                type = "PlayerState",
                data = JsonUtility.ToJson(state)
            };

        SendMessage(message);
    }

    // ---------------------------------------------------------
    // DESCONECTAR
    // ---------------------------------------------------------

    public void Disconnect()
    {
        isConnected = false;

        try
        {
            reader?.Close();
            writer?.Close();
            client?.Close();
        }
        catch
        {
        }

        if (receiveThread != null &&
            receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }

        Debug.Log("Cliente desconectado.");
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}
