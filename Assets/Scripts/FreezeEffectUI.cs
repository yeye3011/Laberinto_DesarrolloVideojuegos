using System.Collections;
using TMPro;
using UnityEngine;

public class FreezeEffectUI : MonoBehaviour
{
    [Header("Pantalla")]
    [SerializeField] private GameObject panelCongelado;

    [Header("Joysticks")]
    [SerializeField] private GameObject joystickNormal;
    [SerializeField] private GameObject joystickCongelado;

    [Header("Mensaje")]
    [SerializeField] private GameObject mensajeCongelar;
    [SerializeField] private TMP_Text textoCongelar;

    private Coroutine freezeUICoroutine;

    private void Start()
    {
        if (panelCongelado != null)
            panelCongelado.SetActive(false);

        if (joystickCongelado != null)
            joystickCongelado.SetActive(false);

        if (mensajeCongelar != null)
            mensajeCongelar.SetActive(false);
    }

    public void MostrarMensaje(string mensaje, float duracion)
    {
        if (freezeUICoroutine != null)
            StopCoroutine(freezeUICoroutine);

        freezeUICoroutine = StartCoroutine(
            MostrarMensajeCoroutine(mensaje, duracion)
        );
    }

    private IEnumerator MostrarMensajeCoroutine(
        string mensaje, float duracion)
    {
        if (mensajeCongelar != null)
            mensajeCongelar.SetActive(true);

        if (textoCongelar != null)
            textoCongelar.text = mensaje;

        yield return new WaitForSeconds(duracion);

        if (mensajeCongelar != null)
            mensajeCongelar.SetActive(false);

        freezeUICoroutine = null;
    }

    public void ActivarVisualCongelado()
    {
        if (panelCongelado != null)
            panelCongelado.SetActive(true);

        if (joystickNormal != null)
            joystickNormal.SetActive(false);

        if (joystickCongelado != null)
            joystickCongelado.SetActive(true);
    }

    public void DesactivarVisualCongelado()
    {
        if (panelCongelado != null)
            panelCongelado.SetActive(false);

        if (joystickNormal != null)
            joystickNormal.SetActive(true);

        if (joystickCongelado != null)
            joystickCongelado.SetActive(false);
    }


    public void IniciarSecuenciaCongelado(string nombreActivador)
    {
        if (freezeUICoroutine != null)
            StopCoroutine(freezeUICoroutine);

        freezeUICoroutine = StartCoroutine(
            SecuenciaCongelado(nombreActivador)
        );
    }

    private IEnumerator SecuenciaCongelado(string nombreActivador)
    {
        ActivarVisualCongelado();

        if (mensajeCongelar != null)
            mensajeCongelar.SetActive(true);

        if (textoCongelar != null)
            textoCongelar.text = nombreActivador + " te ha congelado";

        yield return new WaitForSeconds(2f);

        if (textoCongelar != null)
            textoCongelar.text = "3";

        yield return new WaitForSeconds(1f);

        if (textoCongelar != null)
            textoCongelar.text = "2";

        yield return new WaitForSeconds(1f);

        if (textoCongelar != null)
            textoCongelar.text = "1";

        yield return new WaitForSeconds(1f);

        if (mensajeCongelar != null)
            mensajeCongelar.SetActive(false);

        DesactivarVisualCongelado();
        freezeUICoroutine = null;
    }

}
