
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
            lista = JsonUtility.FromJson<PlayerScoreList>(
                DatosResultados.rankingJSON
            );
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
            textoGanador.text =
                "Ganador: " + ganador.playerName + "   -   Puntos: " + ganador.score;
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

    public void VolverJugar()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
