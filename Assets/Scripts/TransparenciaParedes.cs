
using UnityEngine;
using UnityEngine.UI;

public class TransparenciaParedes : MonoBehaviour
{
    [Header("Paredes")]
    [SerializeField] private Renderer paredesRenderer;
    [SerializeField] private Material materialTransparente;

    [Header("Boton")]
    [SerializeField] private Button botonTransparencia;
    [SerializeField] private Image indicadorRecarga;

    [Header("Iconos del boton")]
    [SerializeField] private Image imagenIconoBoton;
    [SerializeField] private Sprite iconoOjoNormal;
    [SerializeField] private Sprite iconoOjoTachado;

    [Header("Tiempos")]
    [SerializeField] private float duracionTransparencia = 5f;
    [SerializeField] private float duracionRecarga = 25f;

    private Material[] materialesOriginales;

    private float transparenciaRestante;
    private float recargaRestante;

    private bool transparenciaActiva;
    private bool congelado;

    private void Awake()
    {
        if (paredesRenderer == null || materialTransparente == null)
        {

            enabled = false;
            return;
        }

        materialesOriginales = paredesRenderer.sharedMaterials;

        if (botonTransparencia != null)
            botonTransparencia.onClick.AddListener(ActivarTransparencia);

        if (indicadorRecarga != null)
        {
            indicadorRecarga.type = Image.Type.Filled;
            indicadorRecarga.fillMethod = Image.FillMethod.Vertical;
            indicadorRecarga.fillOrigin = (int)Image.OriginVertical.Bottom;
            indicadorRecarga.fillAmount = 1f;
        }

        ActualizarInterfaz();
    }

    private void OnEnable()
    {
        PlayerMovement.FreezeStateChanged += AlCambiarCongelamiento;
    }

    private void OnDisable()
    {
        PlayerMovement.FreezeStateChanged -= AlCambiarCongelamiento;

        RestaurarMaterialOriginal();
    }

    private void Update()
    {
        if (congelado)
            return;

        if (transparenciaActiva)
        {
            transparenciaRestante -= Time.deltaTime;

            if (transparenciaRestante <= 0f)
            {
                transparenciaRestante = 0f;
                transparenciaActiva = false;
                RestaurarMaterialOriginal();
            }
        }

        if (recargaRestante > 0f)
        {
            recargaRestante -= Time.deltaTime;

            if (recargaRestante < 0f)
                recargaRestante = 0f;
        }

        ActualizarInterfaz();
    }

    public void ActivarTransparencia()
    {
        if (congelado || transparenciaActiva || recargaRestante > 0f)
            return;

        transparenciaActiva = true;
        transparenciaRestante = duracionTransparencia;
        recargaRestante = duracionRecarga;

        AplicarMaterialTransparente();
        ActualizarInterfaz();
    }

    private void AlCambiarCongelamiento(PlayerMovement jugador, bool estaCongelado)
    {
        if (jugador == null || !jugador.IsLocalPlayer)
            return;

        congelado = estaCongelado;

        ActualizarInterfaz();
    }

    private void AplicarMaterialTransparente()
    {
        if (paredesRenderer == null || materialTransparente == null)
            return;

        Material[] nuevosMateriales = new Material[
            materialesOriginales.Length
        ];

        for (int i = 0; i < nuevosMateriales.Length; i++)
            nuevosMateriales[i] = materialTransparente;

        paredesRenderer.sharedMaterials = nuevosMateriales;
    }

    private void RestaurarMaterialOriginal()
    {
        if (paredesRenderer != null && materialesOriginales != null)
            paredesRenderer.sharedMaterials = materialesOriginales;
    }

    private void ActualizarInterfaz()
    {
        if (botonTransparencia != null)
        {
            botonTransparencia.interactable =
                !congelado &&
                !transparenciaActiva &&
                recargaRestante <= 0f;
        }

        if (indicadorRecarga != null)
        {
            if (recargaRestante <= 0f)
            {
                indicadorRecarga.fillAmount = 1f;
            }
            else
            {
                indicadorRecarga.fillAmount = 1f - recargaRestante / duracionRecarga;
            }
        }

        if (imagenIconoBoton != null)
        {
            bool disponible = !transparenciaActiva && recargaRestante <= 0f;

            imagenIconoBoton.sprite = disponible
                ? iconoOjoNormal
                : iconoOjoTachado;
        }
    }

    private void OnDestroy()
    {
        if (botonTransparencia != null)
            botonTransparencia.onClick.RemoveListener(
                ActivarTransparencia
            );

        RestaurarMaterialOriginal();
    }
}
