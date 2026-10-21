
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapaMarcadores : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camaraMapa;
    [SerializeField] private RectTransform areaMinimapa;
    [SerializeField] private RectTransform contenedorMarcadores;
    [SerializeField] private Image marcadorPrefab;

    [Header("Tamaño de los marcadores")]
    [SerializeField] private float tamanoJugador = 11f;
    [SerializeField] private float tamanoQueso = 10f;

    [Header("Colores")]
    [SerializeField] private Color colorJugadorLocal = new Color32(67, 166, 107, 255);
    [SerializeField] private Color colorOtrosJugadores = new Color32(149, 99, 199, 255);
    [SerializeField] private Color colorQuesoNormal = new Color32(242, 201, 76, 255);
    [SerializeField] private Color colorQuesoHielo = new Color32(118, 155, 177, 255);
    [SerializeField] private Color colorQuesoVelocidad = new Color32(194, 101, 121, 255);

    private class Marcador
    {
        public Transform objetivo;
        public RectTransform rect;
        public Image imagen;
    }

    private readonly Dictionary<int, Marcador> marcadoresJugadores = new();
    private readonly Dictionary<int, Marcador> marcadoresQuesos = new();

    private Sprite spriteMarcador;
    private int ultimoIdLocal = int.MinValue;

    private void Awake()
    {
        CrearSpriteMarcador();
    }

    private void LateUpdate()
    {
        ActualizarJugadores();
        ActualizarQuesos();
    }

    private void CrearSpriteMarcador()
    {
        const int resolucion = 64;
        Texture2D textura = new Texture2D(
            resolucion,
            resolucion,
            TextureFormat.RGBA32,
            false
        );

        textura.filterMode = FilterMode.Bilinear;
        textura.wrapMode = TextureWrapMode.Clamp;

        Vector2 centro = new Vector2(
            (resolucion - 1) * 0.5f,
            (resolucion - 1) * 0.5f
        );

        float radio = resolucion * 0.46f;
        float radioCuadrado = radio * radio;

        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                float dx = x - centro.x;
                float dy = y - centro.y;

                bool dentro = dx * dx + dy * dy <= radioCuadrado;

                textura.SetPixel(
                    x,
                    y,
                    dentro ? Color.white : Color.clear
                );
            }
        }

        textura.Apply();

        spriteMarcador = Sprite.Create(
            textura,
            new Rect(0, 0, resolucion, resolucion),
            new Vector2(0.5f, 0.5f)
        );
    }


    private void ActualizarJugadores()
    {
        PlayerIdentity[] jugadores = FindObjectsByType<PlayerIdentity>(
            FindObjectsSortMode.None
        );

        HashSet<int> idsVistos = new HashSet<int>();
        int idLocal = ObtenerIdLocal();

        foreach (PlayerIdentity jugador in jugadores)
        {
            if (jugador == null)
                continue;

            int id = jugador.PlayerId;
            idsVistos.Add(id);

            if (!marcadoresJugadores.TryGetValue(id, out Marcador marcador))
            {
                marcador = CrearMarcador(
                    jugador.transform,
                    idLocal == id ? colorJugadorLocal : colorOtrosJugadores,
                    tamanoJugador
                );

                marcadoresJugadores[id] = marcador;
            }

            marcador.objetivo = jugador.transform;
            marcador.imagen.color = idLocal == id
                ? colorJugadorLocal
                : colorOtrosJugadores;

            ActualizarPosicion(marcador);
        }

        List<int> eliminar = new List<int>();

        foreach (var entrada in marcadoresJugadores)
        {
            if (!idsVistos.Contains(entrada.Key))
            {
                if (entrada.Value.rect != null)
                    Destroy(entrada.Value.rect.gameObject);

                eliminar.Add(entrada.Key);
            }
        }

        foreach (int id in eliminar)
            marcadoresJugadores.Remove(id);
    }

    private void ActualizarQuesos()
    {
        NetworkCheeseIdentity[] quesos =
            FindObjectsByType<NetworkCheeseIdentity>(
                FindObjectsSortMode.None
            );

        HashSet<int> idsVistos = new HashSet<int>();

        foreach (NetworkCheeseIdentity queso in quesos)
        {
            if (queso == null)
                continue;

            int id = queso.CheeseId;
            idsVistos.Add(id);

            if (!marcadoresQuesos.TryGetValue(id, out Marcador marcador))
            {
                marcador = CrearMarcador(
                    queso.transform,
                    ObtenerColorQueso(queso.CheeseType),
                    tamanoQueso
                );

                marcadoresQuesos[id] = marcador;
            }

            marcador.objetivo = queso.transform;
            marcador.imagen.color = ObtenerColorQueso(queso.CheeseType);

            ActualizarPosicion(marcador);
        }

        List<int> eliminar = new List<int>();

        foreach (var entrada in marcadoresQuesos)
        {
            if (!idsVistos.Contains(entrada.Key))
            {
                if (entrada.Value.rect != null)
                    Destroy(entrada.Value.rect.gameObject);

                eliminar.Add(entrada.Key);
            }
        }

        foreach (int id in eliminar)
            marcadoresQuesos.Remove(id);
    }

    private Marcador CrearMarcador(
        Transform objetivo,
        Color color,
        float tamano)
    {
        Image imagen;

        if (marcadorPrefab != null)
        {
            imagen = Instantiate(marcadorPrefab, contenedorMarcadores);
        }
        else
        {
            GameObject objeto = new GameObject(
                "Marcador",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            objeto.transform.SetParent(contenedorMarcadores, false);
            imagen = objeto.GetComponent<Image>();
            imagen.sprite = spriteMarcador;
        }

        RectTransform rect = imagen.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(tamano, tamano);
        rect.localScale = Vector3.one;

        imagen.color = color;
        imagen.raycastTarget = false;

        return new Marcador
        {
            objetivo = objetivo,
            rect = rect,
            imagen = imagen
        };
    }

    private void ActualizarPosicion(Marcador marcador)
    {
        if (camaraMapa == null ||
            areaMinimapa == null ||
            contenedorMarcadores == null ||
            marcador.objetivo == null)
            return;

        Vector3 vista = camaraMapa.WorldToViewportPoint(
            marcador.objetivo.position
        );

        bool visible =
            vista.z > 0f &&
            vista.x >= 0f && vista.x <= 1f &&
            vista.y >= 0f && vista.y <= 1f;

        marcador.rect.gameObject.SetActive(visible);

        if (!visible)
            return;

        float x = (vista.x - 0.5f) * areaMinimapa.rect.width;
        float y = (vista.y - 0.5f) * areaMinimapa.rect.height;

        marcador.rect.anchoredPosition = new Vector2(x, y);
    }

    private int ObtenerIdLocal()
    {
        if (NetworkSession.Instance != null)
            return NetworkSession.Instance.LocalPlayerId;

        return int.MinValue;
    }

    private Color ObtenerColorQueso(string tipo)
    {
        switch (tipo)
        {
            case "Ice":
                return colorQuesoHielo;

            case "Speed":
                return colorQuesoVelocidad;

            default:
                return colorQuesoNormal;
        }
    }

    private void OnDestroy()
    {
        if (spriteMarcador != null)
            Destroy(spriteMarcador);
    }
}
