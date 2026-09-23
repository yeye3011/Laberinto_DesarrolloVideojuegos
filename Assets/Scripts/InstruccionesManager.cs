using UnityEngine;
using UnityEngine.SceneManagement;

public class InstruccionesManager : MonoBehaviour
{
    public void Continuar()
    {
        SceneManager.LoadScene("Lobby");
    }
}