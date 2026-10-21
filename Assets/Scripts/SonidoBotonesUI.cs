using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SonidoBotonesUI : MonoBehaviour
{
    private readonly HashSet<Button> botonesRegistrados = new HashSet<Button>();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        RegistrarBotones();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Limpiamos referencias nulas al cambiar de escena y registramos los nuevos botones
        botonesRegistrados.Clear();
        RegistrarBotones();
    }

    public void RegistrarBotones()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button boton in botones)
        {
            if (boton == null || botonesRegistrados.Contains(boton))
            {
                continue;
            }

            boton.onClick.AddListener(ReproducirClic);
            botonesRegistrados.Add(boton);
        }
    }

    private void ReproducirClic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirClic();
        }
    }
}