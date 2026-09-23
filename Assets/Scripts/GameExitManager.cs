using UnityEngine;
using UnityEngine.SceneManagement;

public class GameExitManager : MonoBehaviour
{
    public void SalirAlMenu()
    {
        LobbyServer lobbyServer =
            FindFirstObjectByType<LobbyServer>();

        LobbyClient lobbyClient =
            FindFirstObjectByType<LobbyClient>();

        // Detener servidor si somos el host
        if (lobbyServer != null &&
            lobbyServer.IsRunning)
        {
            lobbyServer.StopServer();
        }

        // Desconectar si somos cliente
        if (lobbyClient != null &&
            lobbyClient.IsConnected)
        {
            lobbyClient.Disconnect();
        }

        // Limpiar datos de la partida
        if (NetworkSession.Instance != null)
        {
            NetworkSession.Instance.ClearPlayers();
            NetworkSession.Instance.SetLocalPlayerId(0);
        }

        // Destruir el NetworkManager persistente
        if (NetworkPersistence.Instance != null)
        {
            Destroy(NetworkPersistence.Instance.gameObject);
        }

        // Volver al menú
        SceneManager.LoadScene("Menu");
    }
}
