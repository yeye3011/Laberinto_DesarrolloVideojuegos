using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    [Header("Ranking")]
    [SerializeField] private TMP_Text textoRanking;

    [Header("Temporizador")]
    [SerializeField] private TMP_Text textoTiempo;

    [Header("Supervelocidad")]
    [SerializeField] private GameObject mensajeVelocidad;
    [SerializeField] private TMP_Text textoSuperVelocidad;

    private Coroutine velocidadHUDCoroutine;
    private bool supervelocidadActiva = false;
    private int jugadorConSupervelocidad = -1;

    [Header("Congelar")]
    private readonly HashSet<int> jugadoresCongelados = new HashSet<int>();
    private readonly Color colorCongelado = new Color32(0x76, 0x9B, 0xB1, 0xFF);

    [Header("Fin de partida")]
    [SerializeField] private GameObject panelFinPartida;
    [SerializeField] private TMP_Text textoFinPartida;

    private LobbyServer lobbyServer;
    private LobbyClient lobbyClient;

    private readonly ConcurrentQueue<Action> accionesPendientes = new ConcurrentQueue<Action>();

    private Coroutine blinkCoroutine;
    private Coroutine endMessageCoroutine;
    private bool finalizando;
    private string ultimoRankingJSON = "";
    private bool partidaCancelada = false;

    private void Start()
    {
        lobbyServer = FindFirstObjectByType<LobbyServer>();
        lobbyClient = FindFirstObjectByType<LobbyClient>();

        if (panelFinPartida != null)
            panelFinPartida.SetActive(false);

        if (textoTiempo != null)
            textoTiempo.text = "2:00";

        if (lobbyServer != null && lobbyServer.IsRunning)
        {
            lobbyServer.OnMatchTimeReceived += RecibirTiempo;
            lobbyServer.OnMatchFinished += RecibirFinPartida;
            lobbyServer.OnMatchCancelled += RecibirCancelacion;
            lobbyServer.OnScoreStateReceived += RecibirRanking;
        }

        if (lobbyServer != null && lobbyServer.IsRunning)
        {
            lobbyServer.BroadcastScoreStateForHUD();
        }

        if (lobbyServer != null && lobbyServer.IsRunning)
        {
            if (lobbyServer.LatestMatchTime >= 0)
            {
                RecibirTiempo(lobbyServer.LatestMatchTime);
            }
        }

        if (lobbyClient != null && lobbyClient.IsConnected)
        {
            lobbyClient.OnMatchTimeReceived += RecibirTiempo;
            lobbyClient.OnMatchFinished += RecibirFinPartida;
            lobbyClient.OnMatchCancelled += RecibirCancelacion;
            lobbyClient.OnScoreStateReceived += RecibirRanking;
        }

        if (lobbyClient != null && lobbyClient.IsConnected)
        {
            if (lobbyClient.LatestMatchTime >= 0)
            {
                RecibirTiempo(lobbyClient.LatestMatchTime);
            }
        }

        if (lobbyClient != null && lobbyClient.IsConnected)
        {
            if (!string.IsNullOrEmpty(lobbyClient.LatestScoreState))
            {
                RecibirRanking(lobbyClient.LatestScoreState);
            }
        }

    }

    private void Update()
    {
        while (accionesPendientes.TryDequeue(out Action accion))
        {
            accion?.Invoke();
        }
    }

    public void MostrarSupervelocidad(int playerId)
    {
        jugadorConSupervelocidad = playerId;

        if (velocidadHUDCoroutine != null)
        {
            StopCoroutine(velocidadHUDCoroutine);
        }

        velocidadHUDCoroutine = StartCoroutine(
            CuentaRegresivaSupervelocidad()
        );

        if (!string.IsNullOrEmpty(ultimoRankingJSON))
        {
            ActualizarRanking(ultimoRankingJSON);
        }
    }

    private IEnumerator CuentaRegresivaSupervelocidad()
    {
        supervelocidadActiva = true;

        if (mensajeVelocidad != null)
            mensajeVelocidad.SetActive(true);

        for (int segundos = 5; segundos >= 1; segundos--)
        {
            if (textoSuperVelocidad != null)
            {
                textoSuperVelocidad.text = "¡SUPERVELOCIDAD! -> " + segundos;
            }

            yield return new WaitForSeconds(1f);
        }

        supervelocidadActiva = false;

        if (mensajeVelocidad != null)
            mensajeVelocidad.SetActive(false);

        jugadorConSupervelocidad = -1;

        if (!string.IsNullOrEmpty(ultimoRankingJSON))
        {
            ActualizarRanking(ultimoRankingJSON);
        }

        velocidadHUDCoroutine = null;
    }

    public void OcultarSupervelocidad()
    {
        supervelocidadActiva = false;

        if (velocidadHUDCoroutine != null)
        {
            StopCoroutine(velocidadHUDCoroutine);
            velocidadHUDCoroutine = null;
        }

        if (mensajeVelocidad != null)
            mensajeVelocidad.SetActive(false);

        jugadorConSupervelocidad = -1;

        if (!string.IsNullOrEmpty(ultimoRankingJSON))
        {
            ActualizarRanking(ultimoRankingJSON);
        }
    }


    private void RecibirTiempo(int segundos)
    {
        accionesPendientes.Enqueue(() => ActualizarTiempo(segundos));
    }

    private void RecibirRanking(string json)
    {
        if (!string.IsNullOrEmpty(json))
            ultimoRankingJSON = json;

        accionesPendientes.Enqueue(() => ActualizarRanking(json));
    }

    private void RecibirFinPartida()
    {
        accionesPendientes.Enqueue(() =>
        {
            partidaCancelada = false;
            MostrarMensajeFinal("¡SE ACABÓ EL JUEGO!");
        });
    }

    private void RecibirCancelacion()
    {
        accionesPendientes.Enqueue(() =>
        {
            partidaCancelada = true;

            MostrarMensajeFinal(
                "Lo sentimos,\nla conexión se ha perdido."
            );
        });
    }


    private void ActualizarTiempo(int segundos)
    {
        if (textoTiempo == null || finalizando)
            return;

        segundos = Mathf.Max(0, segundos);

        int minutos = segundos / 60;
        int segundosRestantes = segundos % 60;

        textoTiempo.text =
            minutos.ToString("0") + ":" +
            segundosRestantes.ToString("00");

        if (segundos <= 30 && segundos > 0)
        {
            if (blinkCoroutine == null)
                blinkCoroutine = StartCoroutine(ParpadearTiempo());
        }
        else
        {
            DetenerParpadeo();

            if (textoTiempo != null)
                textoTiempo.alpha = 1f;
        }
    }

    private IEnumerator ParpadearTiempo()
    {
        Color colorOriginal = new Color32(0x4A, 0x31, 0x1E, 0xFF);
        Color rojo = Color.red;

        while (!finalizando && textoTiempo != null)
        {
            textoTiempo.color = rojo;
            textoTiempo.alpha = 1f;
            yield return new WaitForSeconds(0.35f);

            textoTiempo.color = colorOriginal;
            textoTiempo.alpha = 0.8f;
            yield return new WaitForSeconds(0.35f);
        }

        if (textoTiempo != null)
        {
            textoTiempo.color = colorOriginal;
            textoTiempo.alpha = 1f;
        }

        blinkCoroutine = null;
    }

    private void DetenerParpadeo()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (textoTiempo != null)
        {
            textoTiempo.color = new Color32(0x4A, 0x31, 0x1E, 0xFF);
            textoTiempo.alpha = 1f;
        }
    }

    public void ActualizarJugadoresCongelados(int[] playerIds)
    {
        if (playerIds != null)
        {
            foreach (int playerId in playerIds)
            {
                jugadoresCongelados.Add(playerId);
            }
        }

        if (!string.IsNullOrEmpty(ultimoRankingJSON))
        {
            ActualizarRanking(ultimoRankingJSON);
        }
    }

    public void QuitarJugadorCongelado(int playerId)
    {
        jugadoresCongelados.Remove(playerId);

        if (!string.IsNullOrEmpty(ultimoRankingJSON))
        {
            ActualizarRanking(ultimoRankingJSON);
        }
    }


    private void ActualizarRanking(string json)
    {
        if (textoRanking == null || string.IsNullOrEmpty(json))
            return;

        PlayerScoreList lista;

        try
        {
            lista = JsonUtility.FromJson<PlayerScoreList>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameHUD: error leyendo ranking: " + e.Message);
            return;
        }

        if (lista == null || lista.players == null)
            return;

        List<PlayerScoreData> ordenados = lista.players
            .Where(p => p != null)
            .OrderByDescending(p => p.score)
            .ThenBy(p => p.playerName)
            .ToList();


        string colorVelocidad = "#C26579";

        string colorHielo = "#769BB1";

        textoRanking.text = string.Join(
            "\n",
            ordenados.Select(p =>
            {
                string linea = p.playerName + ":  " + p.score;

                if (jugadoresCongelados.Contains(p.playerId))
                {
                    return "<color=" + colorHielo + ">" + linea + "</color>";
                }

                if (p.playerId == jugadorConSupervelocidad)
                {
                    return "<color=" + colorVelocidad + ">" + linea + "</color>";
                }

                return linea;
            })
        );
    }

    private void MostrarMensajeFinal(string mensaje)
    {
        if (finalizando)
            return;

        finalizando = true;

        DatosResultados.rankingJSON = ultimoRankingJSON;
        DatosResultados.partidaCancelada = partidaCancelada;
        DatosResultados.mensajeFinal = mensaje;

        DetenerParpadeo();
        OcultarSupervelocidad();

        if (endMessageCoroutine != null)
            StopCoroutine(endMessageCoroutine);

        endMessageCoroutine = StartCoroutine(MostrarMensajeFinalCoroutine(mensaje));
    }

    private IEnumerator MostrarMensajeFinalCoroutine(string mensaje)
    {
        if (panelFinPartida != null)
            panelFinPartida.SetActive(true);

        if (textoFinPartida != null)
            textoFinPartida.text = mensaje;

        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene("ResultadosScene");
    }

    private void OnDestroy()
    {
        if (lobbyServer != null)
        {
            lobbyServer.OnMatchTimeReceived -= RecibirTiempo;
            lobbyServer.OnMatchFinished -= RecibirFinPartida;
            lobbyServer.OnMatchCancelled -= RecibirCancelacion;
            lobbyServer.OnScoreStateReceived -= RecibirRanking;
        }

        if (lobbyClient != null)
        {
            lobbyClient.OnMatchTimeReceived -= RecibirTiempo;
            lobbyClient.OnMatchFinished -= RecibirFinPartida;
            lobbyClient.OnMatchCancelled -= RecibirCancelacion;
            lobbyClient.OnScoreStateReceived -= RecibirRanking;
        }

        DetenerParpadeo();
    }
}
