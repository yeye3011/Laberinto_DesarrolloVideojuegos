using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadosManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject resultadosPodio;
    [SerializeField] private GameObject conexionPerdida;

    [Header("Textos del podio")]
    [SerializeField] private TMP_Text tituloResultados;
    [SerializeField] private TMP_Text textoGanador;
    [SerializeField] private TMP_Text rankingFinal;

    [Header("Botón volver a jugar")]
    [SerializeField] private UnityEngine.UI.Button botonVolverJugar;
    [SerializeField] private CanvasGroup transparenciaBoton;

    private LobbyClient clienteSuscrito;

    [Header("Aviso de desconexión")]
    [SerializeField] private TMP_Text tituloConexion;
    [SerializeField] private TMP_Text mensajeConexion;

    private void Start()
    {
        bool cancelada = DatosResultados.partidaCancelada;

        if (resultadosPodio != null)
            resultadosPodio.SetActive(!cancelada);

        if (conexionPerdida != null)
            conexionPerdida.SetActive(cancelada);

        if (cancelada)
        {
            if (tituloConexion != null)
                tituloConexion.text = "¡Partida interrumpida!";

            if (mensajeConexion != null)
            {
                mensajeConexion.text =
                    string.IsNullOrEmpty(DatosResultados.mensajeFinal)
                    ? "Lo sentimos, la conexión se ha perdido."
                    : DatosResultados.mensajeFinal;
            }

            return;
        }

        MostrarPodio();
    }

    private void MostrarPodio()
    {
        if (tituloResultados != null)
            tituloResultados.text = "Podio";

        if (string.IsNullOrEmpty(DatosResultados.rankingJSON))
        {
            if (textoGanador != null)
                textoGanador.text = "No hay resultados disponibles.";

            if (rankingFinal != null)
                rankingFinal.text = "";

            return;
        }

        PlayerScoreList lista;

        try
        {
            lista = JsonUtility.FromJson<PlayerScoreList>(DatosResultados.rankingJSON);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "Error leyendo resultados: " + e.Message
            );
            return;
        }

        if (lista == null || lista.players == null)
            return;

        List<PlayerScoreData> ordenados = lista.players
            .Where(p => p != null)
            .OrderByDescending(p => p.score)
            .ThenBy(p => p.playerName)
            .ToList();

        if (ordenados.Count == 0)
        {
            if (textoGanador != null)
                textoGanador.text = "No hay jugadores en el podio.";

            if (rankingFinal != null)
                rankingFinal.text = "";

            return;
        }


        PlayerScoreData ganador = ordenados[0];

        if (textoGanador != null)
        {
            int jugadoresConMayorPuntaje = ordenados.Count(p => p.score == ganador.score);

            if (jugadoresConMayorPuntaje > 1)
            {
                textoGanador.text = "¡Quedaron en empate!\nNo hay ganador.";
            }
            else
            {
                textoGanador.text = "Ganador: " + ganador.playerName + "   -   Puntos: " + ganador.score;
            }
        }

        if (rankingFinal != null)
        {
            rankingFinal.text = string.Join(
                "\n",
                ordenados.Select((p, i) =>
                    (i + 1) + ". " + p.playerName +
                    "  —  " + p.score + " puntos"
                )
            );
        }
    }

    private void OnEnable()
    {
        ActualizarBotonVolverJugar();
    }

    private void OnDisable()
    {
        if (clienteSuscrito != null)
        {
            clienteSuscrito.OnHostReturnedToLobby -= HabilitarBotonVolverJugar;
            clienteSuscrito = null;
        }
    }

    private void ActualizarBotonVolverJugar()
    {
        if (NetworkPersistence.Instance == null)
            return;

        LobbyServer servidor =
            NetworkPersistence.Instance.GetComponent<LobbyServer>();

        LobbyClient cliente =
            NetworkPersistence.Instance.GetComponent<LobbyClient>();

        // El host puede volver cuando quiera.
        if (servidor != null && servidor.IsRunning)
        {
            AplicarEstadoBoton(true);
            return;
        }

        // El cliente puede volver cuando el host ya regresó.
        if (cliente != null && cliente.HostHasReturnedToLobby)
        {
            AplicarEstadoBoton(true);
            return;
        }

        // Si el host aún no ha regresado, esperar el aviso.
        AplicarEstadoBoton(false);

        if (cliente != null)
        {
            clienteSuscrito = cliente;
            clienteSuscrito.OnHostReturnedToLobby += HabilitarBotonVolverJugar;
        }
    }

    private void HabilitarBotonVolverJugar()
    {
        AplicarEstadoBoton(true);
    }

    private void AplicarEstadoBoton(bool habilitado)
    {
        if (botonVolverJugar != null)
            botonVolverJugar.interactable = habilitado;

        if (transparenciaBoton != null)
            transparenciaBoton.alpha = habilitado ? 1f : 0.4f;

        if (transparenciaBoton != null)
            transparenciaBoton.interactable = habilitado;
    }

    public void VolverJugar()
    {
        Debug.Log("VOLVER A JUGAR: preparando regreso al lobby.");

        LobbyServer servidor = FindFirstObjectByType<LobbyServer>();
        LobbyClient cliente = FindFirstObjectByType<LobbyClient>();

        Debug.Log(
            "DIAGNÓSTICO ANTES DE VOLVER AL LOBBY | " +
            "Persistence: " + (NetworkPersistence.Instance != null) +
            " | Server encontrado: " + (servidor != null) +
            " | Server activo: " + (servidor != null && servidor.IsRunning) +
            " | Client encontrado: " + (cliente != null) +
            " | Client conectado: " + (cliente != null && cliente.IsConnected)
        );

        if (NetworkPersistence.Instance != null)
        {
            servidor = NetworkPersistence.Instance.GetComponent<LobbyServer>();
            cliente = NetworkPersistence.Instance.GetComponent<LobbyClient>();

            if (servidor != null && servidor.IsRunning)
            {
                servidor.PrepararRegresoAlLobby();
            }
            else if (cliente != null && cliente.IsConnected)
            {
                cliente.SendReturnToLobby();
            }
        }

        LobbyUI.PrepararVolverASalaDeEspera();

        SceneManager.LoadScene("Lobby");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
