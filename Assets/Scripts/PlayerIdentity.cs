using TMPro;
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] private int playerId;
    [SerializeField] private string playerName;

    [Header("Nombre sobre la ratita")]
    [SerializeField] private TMP_Text nombreJugadorTexto;

    public int PlayerId => playerId;
    public string PlayerName => playerName;

    public void SetIdentity(int id, string name)
    {
        playerId = id;
        playerName = name;

        gameObject.name = "Player_Raton_" + id;

        ActualizarNombreVisual();
    }

    private void ActualizarNombreVisual()
    {
        if (nombreJugadorTexto != null)
        {
            nombreJugadorTexto.text = playerName;
        }
    }
}