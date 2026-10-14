using UnityEngine;
using UnityEngine.SceneManagement;

public class InstruccionesManager : MonoBehaviour
{
    [Header("Configuración de Paneles")]
    public GameObject[] panelesDeInstrucciones;
    public GameObject botonSiguiente; 

    private int indiceActual = 0;

    void Start()
    {
        // Al iniciar, mostramos únicamente el primer panel
        ActualizarPaneles();
    }

    public void SiguientePanel()
    {
        if (indiceActual < panelesDeInstrucciones.Length - 1)
        {
            indiceActual++;
            ActualizarPaneles();
        }
    }

    private void ActualizarPaneles()
    {
        for (int i = 0; i < panelesDeInstrucciones.Length; i++)
        {
            if (panelesDeInstrucciones[i] != null)
            {
                panelesDeInstrucciones[i].SetActive(i == indiceActual);
            }
        }

        if (indiceActual == panelesDeInstrucciones.Length - 1)
        {
            if (botonSiguiente != null)
            {
                botonSiguiente.SetActive(false);
            }
        }
    }

    public void Continuar()
    {
        SceneManager.LoadScene("Lobby");
    }
}