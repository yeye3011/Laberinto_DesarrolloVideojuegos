using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Configuración del Botón")]
    public RectTransform botonIniciar;
    public float velocidadPulsacion = 3f;
    public float escalaMaxima = 1.15f;

    [Header("Efecto de Sonido")]
    public AudioSource audioSource; // Componente AudioSource
    public AudioClip sonidoClic;    // Tu clip de audio 'BotonInicio'

    private Vector3 escalaOriginal;

    void Start()
    {
        if (botonIniciar != null)
        {
            escalaOriginal = botonIniciar.localScale;
        }

        // Si no asignas un AudioSource manualmente, intentará obtener uno en el objeto
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (botonIniciar != null)
        {
            float escala = 1f + (Mathf.Sin(Time.time * velocidadPulsacion) * (escalaMaxima - 1f));
            botonIniciar.localScale = escalaOriginal * escala;
        }
    }

    public void Iniciar()
    {
        StartCoroutine(ReproducirSonidoYCambiarEscena());
    }

    private IEnumerator ReproducirSonidoYCambiarEscena()
    {
        if (audioSource != null && sonidoClic != null)
        {
            audioSource.PlayOneShot(sonidoClic);
            yield return new WaitForSeconds(0.1f);
        }

        SceneManager.LoadScene("Instrucciones");
    }
}