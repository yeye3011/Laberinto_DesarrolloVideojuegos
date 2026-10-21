
using UnityEngine;
using UnityEngine.Rendering;

public class CicloDiaNoche : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Light luzPrincipal;
    [SerializeField] private Material skyboxDia;

    [Header("Duracion del ciclo")]
    [SerializeField] private float duracionCiclo = 125f;

    [Header("Luz")]
    [SerializeField] private float intensidadDia = 2.5f;
    [SerializeField] private float intensidadNoche = 1.5f;

    [Header("Ambiente claro")]
    [SerializeField]
    private Color luzDia = new Color(1f, 0.91f, 0.82f);

    [SerializeField]
    private Color luzNoche = new Color(0.68f, 0.62f, 0.88f);

    [SerializeField]
    private Color cieloDia = new Color(0.72f, 0.84f, 1f);

    [SerializeField]
    private Color cieloNoche = new Color(0.40f, 0.34f, 0.63f);

    [SerializeField]
    private Color sueloDia = new Color(0.72f, 0.69f, 0.68f);

    [SerializeField]
    private Color sueloNoche = new Color(0.43f, 0.39f, 0.55f);

    [SerializeField] private float exposicionDia = 1.3f;
    [SerializeField] private float exposicionNoche = 0.35f;
    [SerializeField] private float atmosferaDia = 1f;
    [SerializeField] private float atmosferaNoche = 0.6f;

    private static readonly int AtmosphereID = Shader.PropertyToID("_AtmosphereThickness");

    private Material skyboxOriginal;
    private Material skyboxInstancia;

    private Light luzSolOriginal;

    private Color ambienteOriginal;
    private AmbientMode modoAmbienteOriginal;

    private float intensidadOriginal;
    private Color colorOriginal;

    private float tiempo;
    private bool configurado;

    private static readonly int SkyTintID = Shader.PropertyToID("_SkyTint");

    private static readonly int GroundColorID = Shader.PropertyToID("_GroundColor");

    private static readonly int ExposureID = Shader.PropertyToID("_Exposure");

    private void Start()
    {
        if (luzPrincipal == null || skyboxDia == null)
        {
            enabled = false;
            return;
        }

        if (skyboxDia.shader == null ||
            skyboxDia.shader.name != "Skybox/Procedural")
        {
            enabled = false;
            return;
        }

        skyboxOriginal = RenderSettings.skybox;
        luzSolOriginal = RenderSettings.sun;

        ambienteOriginal = RenderSettings.ambientLight;
        modoAmbienteOriginal = RenderSettings.ambientMode;

        intensidadOriginal = luzPrincipal.intensity;
        colorOriginal = luzPrincipal.color;

        // Crear una copia para no modificar el material del proyecto.
        skyboxInstancia = new Material(skyboxDia);
        skyboxInstancia.name = "Skybox_DiaNoche_Runtime";

        RenderSettings.skybox = skyboxInstancia;
        RenderSettings.sun = luzPrincipal;

        // Mantener una iluminacion ambiental
        RenderSettings.ambientMode = AmbientMode.Flat;

        tiempo = 0f;
        configurado = true;

        ActualizarCiclo(0f);
    }

    private void Update()
    {
        if (!configurado)
            return;

        tiempo += Time.deltaTime;

        float progreso = Mathf.Clamp01(tiempo / duracionCiclo);

        ActualizarCiclo(progreso);
    }

    private void ActualizarCiclo(float progreso)
    {
        // Transicion gradual de dia a noche.
        float t = Mathf.SmoothStep(0f, 1f, progreso);

        luzPrincipal.intensity = Mathf.Lerp(
            intensidadDia, intensidadNoche, t
        );

        luzPrincipal.color = Color.Lerp(luzDia, luzNoche, t);

        if (skyboxInstancia.HasProperty(SkyTintID))
        {
            skyboxInstancia.SetColor(
                SkyTintID, Color.Lerp(cieloDia, cieloNoche, t)
            );
        }

        if (skyboxInstancia.HasProperty(GroundColorID))
        {
            skyboxInstancia.SetColor(
                GroundColorID, Color.Lerp(sueloDia, sueloNoche, t)
            );
        }

        if (skyboxInstancia.HasProperty(ExposureID))
        {
            skyboxInstancia.SetFloat(ExposureID, 1.3f);
        }

        // Evitar que el laberinto quede demasiado oscuro.
        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.78f, 0.78f, 0.82f),
            new Color(0.48f, 0.44f, 0.62f),
            t
        );

        skyboxInstancia.SetFloat(ExposureID, Mathf.Lerp(exposicionDia, exposicionNoche, t));
        skyboxInstancia.SetFloat(AtmosphereID, Mathf.Lerp(atmosferaDia, atmosferaNoche, t));
    }

    private void OnDestroy()
    {
        if (!configurado)
            return;

        // Restaurar la configuracion original al salir de MainScene.
        if (RenderSettings.skybox == skyboxInstancia)
            RenderSettings.skybox = skyboxOriginal;

        if (RenderSettings.sun == luzPrincipal)
            RenderSettings.sun = luzSolOriginal;

        RenderSettings.ambientLight = ambienteOriginal;
        RenderSettings.ambientMode = modoAmbienteOriginal;

        if (luzPrincipal != null)
        {
            luzPrincipal.intensity = intensidadOriginal;
            luzPrincipal.color = colorOriginal;
        }

        if (skyboxInstancia != null)
            Destroy(skyboxInstancia);
    }
}
