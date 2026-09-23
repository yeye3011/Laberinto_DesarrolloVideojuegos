using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [Header("Host")]
    [SerializeField] private TMP_InputField hostNameInput;
    [SerializeField] private LobbyServer lobbyServer;
    [SerializeField] private Button iniciarPartidaButton;
    [SerializeField] private TMP_Text textoIniciarPartida;
    [SerializeField] private TMP_Text hostIPText;
    [SerializeField] private TMP_Text mensajeIPCopiada;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private LobbyClient lobbyClient;
    [SerializeField] private TMP_Text mensajeEspera;

    [Header("Cliente")]
    [SerializeField] private TMP_InputField clientNameInput;
    [SerializeField] private TMP_InputField clientIPInput;
    [SerializeField] private TMP_Text mensajeSalaNoDisponible;
    [SerializeField] private TMP_Text clientPlayerCountText;
    [SerializeField] private TMP_Text clientPlayerListText;
    [SerializeField] private TMP_Text mensajeEsperaCliente;

    private bool nombreHostTieneError = false;
    private string listaJugadoresPendiente = null;
    private string cuentaRegresivaPendiente = null;
    private bool conexionFallidaPendiente = false;
    private bool conexionExitosaPendiente = false;
    private Image fondoInput;

    private Image fondoNombreCliente;
    private Image fondoIPCliente;

    private bool nombreClienteTieneError = false;
    private bool ipClienteTieneError = false;

    private Color colorOriginalTextoBoton;

    private float tiempoPuntos = 0f;
    private int cantidadPuntos = 0;

    [Header("Paneles")]
    [SerializeField] private GameObject seleccionModo;
    [SerializeField] private GameObject hostInicio;
    [SerializeField] private GameObject hostEspera;
    [SerializeField] private GameObject jugadoresInicio;
    [SerializeField] private GameObject jugadoresEspera;
    [SerializeField] private GameObject cuentaRegresiva;
    [SerializeField] private TMP_Text mensajeCuentaRegresiva;

    [Header("Audio")]
    [SerializeField] private AudioSource audioCuentaRegresiva;

    private void OnEnable()
    {
        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerListReceived += RecibirListaJugadores;
            lobbyClient.OnConnectionFailed += RecibirConexionFallida;
            lobbyClient.OnConnectionSucceeded += RecibirConexionExitosa;
            lobbyClient.OnCountdownMessageReceived += RecibirCuentaRegresiva;
            lobbyClient.OnSpawnAssignmentsReceived += RecibirAsignacionSpawns;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerListChanged += RecibirListaJugadores;
            lobbyServer.OnCountdownMessage += RecibirCuentaRegresiva;
        }
    }

    private void OnDisable()
    {
        if (lobbyClient != null)
        {
            lobbyClient.OnPlayerListReceived -= RecibirListaJugadores;
            lobbyClient.OnConnectionFailed -= RecibirConexionFallida;
            lobbyClient.OnConnectionSucceeded -= RecibirConexionExitosa;
            lobbyClient.OnCountdownMessageReceived -= RecibirCuentaRegresiva;
            lobbyClient.OnSpawnAssignmentsReceived -= RecibirAsignacionSpawns;
        }

        if (lobbyServer != null)
        {
            lobbyServer.OnPlayerListChanged -= RecibirListaJugadores;
            lobbyServer.OnCountdownMessage -= RecibirCuentaRegresiva;
        }
    }

    private void RecibirListaJugadores(string playerData)
    {
        listaJugadoresPendiente = playerData;
    }

    private void RecibirConexionFallida()
    {
        conexionFallidaPendiente = true;
    }

    private void RecibirConexionExitosa()
    {
        conexionExitosaPendiente = true;
    }

    private void RecibirCuentaRegresiva(string mensaje)
    {
        cuentaRegresivaPendiente = mensaje;
    }

    private void RecibirAsignacionSpawns(string json)
    {
        if (NetworkSession.Instance == null)
        {
            Debug.LogError(
                "No existe NetworkSession."
            );

            return;
        }

        NetworkSession.Instance.LoadPlayersJson(json);

        Debug.Log(
            "Asignación de spawns guardada en NetworkSession."
        );
    }

    private void Update()
    {
        if (listaJugadoresPendiente != null)
        {
            ActualizarListaJugadores(listaJugadoresPendiente);
            listaJugadoresPendiente = null;
        }

        if (conexionFallidaPendiente)
        {
            MostrarConexionFallida();
            conexionFallidaPendiente = false;
        }

        if (conexionExitosaPendiente)
        {
            MostrarEsperaCliente();
            conexionExitosaPendiente = false;
        }

        if (cuentaRegresivaPendiente != null)
        {
            MostrarCuentaRegresiva(cuentaRegresivaPendiente);
            cuentaRegresivaPendiente = null;
        }

        tiempoPuntos += Time.deltaTime;

        if (tiempoPuntos >= 0.5f)
        {
            tiempoPuntos = 0f;

            cantidadPuntos++;

            if (cantidadPuntos > 3)
            {
                cantidadPuntos = 0;
            }

            if (mensajeEspera != null)
            {
                mensajeEspera.text =
                    "Esperando jugadores" +
                    new string('.', cantidadPuntos);
            }

            if (mensajeEsperaCliente != null)
            {
                mensajeEsperaCliente.text =
                    "Esperando que empiece la partida" +
                    new string('.', cantidadPuntos);
            }
        }
    }

    private void MostrarConexionFallida()
    {
        if (mensajeSalaNoDisponible != null)
        {
            mensajeSalaNoDisponible.text =
                "No se encontró una sala disponible.";

            mensajeSalaNoDisponible.gameObject.SetActive(true);
        }
    }

    private void MostrarEsperaCliente()
    {
        if (mensajeSalaNoDisponible != null)
        {
            mensajeSalaNoDisponible.gameObject.SetActive(false);
        }

        jugadoresInicio.SetActive(false);
        jugadoresEspera.SetActive(true);
    }

    private void MostrarCuentaRegresiva(string mensaje)
    {
        if (mensaje == "PREPARADOS")
        {
            if (audioCuentaRegresiva != null)
            {
                audioCuentaRegresiva.Stop();
                audioCuentaRegresiva.Play();
            }
        }

        if (hostEspera != null)
            hostEspera.SetActive(false);

        if (jugadoresEspera != null)
            jugadoresEspera.SetActive(false);

        if (cuentaRegresiva != null)
            cuentaRegresiva.SetActive(true);

        if (mensajeCuentaRegresiva != null)
            mensajeCuentaRegresiva.text = mensaje;

        if (mensaje == "¡A JUGAR!")
        {
            StartCoroutine(EsperarYEntrarAPartida());
        }
    }

    private IEnumerator EsperarYEntrarAPartida()
    {
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("MainScene");
    }

    private void Awake()
    {
        if (hostNameInput != null)
        {
            fondoInput = hostNameInput.GetComponent<Image>();
            hostNameInput.onValueChanged.AddListener(NombreHostEscrito);
        }

        if (clientNameInput != null)
        {
            fondoNombreCliente =
                clientNameInput.GetComponent<Image>();

            clientNameInput.onValueChanged.AddListener(
                NombreClienteEscrito
            );
        }

        if (clientIPInput != null)
        {
            fondoIPCliente =
                clientIPInput.GetComponent<Image>();

            clientIPInput.onValueChanged.AddListener(
                IPClienteEscrita
            );
        }

        if (mensajeSalaNoDisponible != null)
        {
            mensajeSalaNoDisponible.gameObject.SetActive(false);
        }

        if (iniciarPartidaButton != null)
        {
            iniciarPartidaButton.interactable = false;
        }

        if (textoIniciarPartida != null)
        {
            colorOriginalTextoBoton = textoIniciarPartida.color;
        }

        if (cuentaRegresiva != null)
        {
            cuentaRegresiva.SetActive(false);
        }
    }

    public void CrearSala()
    {
        string playerName = hostNameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            nombreHostTieneError = true;

            if (fondoInput != null)
            {
                // Aplicamos el tinte rojo de advertencia
                fondoInput.color = Color.red;
            }

            hostNameInput.Select();
            hostNameInput.ActivateInputField();

            return;
        }

        // Si hay texto válido, aseguramos que el color vuelva a la normalidad
        RestaurarColorInput();

        lobbyServer.StartServer(playerName);

        hostIPText.text = "IP de la Sala: " + lobbyServer.LocalIPAddress;
        playerCountText.text = "Jugadores: 1 / 4";
        playerListText.text = "Tú (" + playerName + ")";

        if (iniciarPartidaButton != null)
        {
            iniciarPartidaButton.interactable = false;
            ActualizarAparienciaBoton();
        }

        hostInicio.SetActive(false);
        hostEspera.SetActive(true);
    }

    private void ActualizarListaJugadores(string playerData)
    {
        string[] jugadores = playerData.Split(
            new char[] { ';' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        string cantidad = "Jugadores: " + jugadores.Length + " / 4";

        if (iniciarPartidaButton != null)
        {
            iniciarPartidaButton.interactable = jugadores.Length >= 2;
            ActualizarAparienciaBoton();
        }

        string lista = "";

        foreach (string jugador in jugadores)
        {
            string[] datos = jugador.Split('|');

            if (datos.Length >= 2)
            {
                lista += datos[1] + "\n";
            }
        }

        // Actualizar información del host
        if (playerCountText != null)
        {
            playerCountText.text = cantidad;
        }

        if (playerListText != null)
        {
            playerListText.text = lista;
        }

        // Actualizar información del cliente
        if (clientPlayerCountText != null)
        {
            clientPlayerCountText.text = cantidad;
        }

        if (clientPlayerListText != null)
        {
            clientPlayerListText.text = lista;
        }
    }

    public void UnirseSala()
    {
        string playerName = clientNameInput.text.Trim();
        string hostIP = clientIPInput.text.Trim();

        bool nombreValido = !string.IsNullOrEmpty(playerName);
        bool ipValida = !string.IsNullOrEmpty(hostIP);

        if (!nombreValido)
        {
            nombreClienteTieneError = true;

            if (fondoNombreCliente != null)
            {
                fondoNombreCliente.color = Color.red;
            }

            clientNameInput.Select();
            clientNameInput.ActivateInputField();
        }

        if (!ipValida)
        {
            ipClienteTieneError = true;

            if (fondoIPCliente != null)
            {
                fondoIPCliente.color = Color.red;
            }

            if (nombreValido)
            {
                clientIPInput.Select();
                clientIPInput.ActivateInputField();
            }
        }

        if (!nombreValido || !ipValida)
        {
            return;
        }

        if (mensajeSalaNoDisponible != null)
        {
            mensajeSalaNoDisponible.gameObject.SetActive(false);
        }

        lobbyClient.ConnectToServer(hostIP, playerName);
    }

    public void NombreHostEscrito(string texto)
    {
        // Si no está en estado de error, no hacemos nada
        if (!nombreHostTieneError)
            return;

        // Apenas el usuario escriba al menos un carácter, quitamos el rojo
        if (!string.IsNullOrWhiteSpace(texto))
        {
            RestaurarColorInput();
        }
    }

    public void NombreClienteEscrito(string texto)
    {
        if (!nombreClienteTieneError)
            return;

        if (!string.IsNullOrWhiteSpace(texto))
        {
            RestaurarColorNombreCliente();
        }
    }

    public void IPClienteEscrita(string texto)
    {
        if (!ipClienteTieneError)
            return;

        if (!string.IsNullOrWhiteSpace(texto))
        {
            RestaurarColorIPCliente();
        }
    }

    public void IniciarPartida()
    {
        if (lobbyServer == null)
            return;

        lobbyServer.StartCountdown();
    }

    public void VolverAlMenu()
    {
        // 1. Cerrar el servidor si somos el host.
        if (lobbyServer != null &&
            lobbyServer.IsRunning)
        {
            lobbyServer.StopServer();
        }

        // 2. Cerrar la conexión si somos cliente.
        if (lobbyClient != null &&
            lobbyClient.IsConnected)
        {
            lobbyClient.Disconnect();
        }

        // 3. Limpiar los datos de la partida.
        if (NetworkSession.Instance != null)
        {
            NetworkSession.Instance.ClearPlayers();
            NetworkSession.Instance.SetLocalPlayerId(0);
        }

        // 4. Destruir el NetworkManager persistente.
        if (NetworkPersistence.Instance != null)
        {
            Destroy(NetworkPersistence.Instance.gameObject);
        }

        // 5. Volver al menú.
        SceneManager.LoadScene("Menu");
    }

    public void VolverDesdeSeleccionModo()
    {
        if (NetworkPersistence.Instance != null)
        {
            Destroy(NetworkPersistence.Instance.gameObject);
        }

        SceneManager.LoadScene("Menu");
    }

    private void RestaurarColorNombreCliente()
    {
        if (fondoNombreCliente != null)
        {
            fondoNombreCliente.color = Color.white;
        }

        nombreClienteTieneError = false;
    }

    private void RestaurarColorIPCliente()
    {
        if (fondoIPCliente != null)
        {
            fondoIPCliente.color = Color.white;
        }

        ipClienteTieneError = false;
    }

    private void RestaurarColorInput()
    {
        if (fondoInput != null)
        {
            fondoInput.color = Color.white;
        }
        nombreHostTieneError = false;
    }

    public void CopiarIP()
    {
        GUIUtility.systemCopyBuffer = lobbyServer.LocalIPAddress;

        if (mensajeIPCopiada != null)
        {
            mensajeIPCopiada.text = "¡IP copiada correctamente!";
            mensajeIPCopiada.gameObject.SetActive(true);
            CancelInvoke(nameof(OcultarMensajeIP));
            Invoke(nameof(OcultarMensajeIP), 2f);
        }
    }

    private void ActualizarAparienciaBoton()
    {
        if (iniciarPartidaButton == null ||
            textoIniciarPartida == null)
        {
            return;
        }

        Color nuevoColor = colorOriginalTextoBoton;

        if (iniciarPartidaButton.interactable)
        {
            nuevoColor.a = 1f;
        }
        else
        {
            nuevoColor.a = 0.35f;
        }

        textoIniciarPartida.color = nuevoColor;
    }

    private void OcultarMensajeIP()
    {
        if (mensajeIPCopiada != null)
        {
            mensajeIPCopiada.gameObject.SetActive(false);
        }
    }
    public void MostrarHostInicio()
    {
        seleccionModo.SetActive(false);
        hostInicio.SetActive(true);
    }

    public void MostrarJugadoresInicio()
    {
        seleccionModo.SetActive(false);
        jugadoresInicio.SetActive(true);
    }
}