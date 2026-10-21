
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioClip sonidoAmbiente;
    [SerializeField] private AudioClip musicaFondo;

    [Header("Efectos")]
    [SerializeField] private AudioClip sonidoClic;
    [SerializeField] private AudioClip quesoNormal;
    [SerializeField] private AudioClip powerupCongelado;
    [SerializeField] private AudioClip powerupVelocidad;

    private AudioSource musicaSource;
    private AudioSource efectosSource;

    private string escenaActual;

    public AudioClip QuesoNormal => quesoNormal;
    public AudioClip PowerupCongelado => powerupCongelado;
    public AudioClip PowerupVelocidad => powerupVelocidad;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
        {
            musicaSource = gameObject.AddComponent<AudioSource>();
            efectosSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            musicaSource = sources[0];
            efectosSource = sources[1];
        }

        musicaSource.loop = true;
        musicaSource.playOnAwake = false;
        efectosSource.playOnAwake = false;

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void Start()
    {
        CambiarMusica(SceneManager.GetActiveScene().name);
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        CambiarMusica(escena.name);
    }

    private void CambiarMusica(string nombreEscena)
    {
        escenaActual = nombreEscena;

        AudioClip musicaDeseada = null;

        switch (nombreEscena)
        {
            case "Menu":
            case "Instrucciones":
            case "Lobby":
            case "ResultadosScene":
                musicaDeseada = sonidoAmbiente;
                break;

            case "MainScene":
                musicaDeseada = musicaFondo;
                break;
        }

        if (musicaSource.clip == musicaDeseada &&
            musicaSource.isPlaying)
        {
            return;
        }

        musicaSource.Stop();
        musicaSource.clip = musicaDeseada;

        if (musicaDeseada != null)
        {
            musicaSource.Play();
        }
    }

    public void ReproducirClic()
    {
        ReproducirEfecto(sonidoClic);
    }

    public void ReproducirEfecto(AudioClip clip)
    {
        if (clip != null)
        {
            efectosSource.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        Instance = null;
    }
}
