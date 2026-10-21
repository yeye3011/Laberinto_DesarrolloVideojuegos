using UnityEngine;

public class NetworkPersistence : MonoBehaviour
{
    public static NetworkPersistence Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Debug.LogError(
            "NETWORK PERSISTENCE DESTRUIDO" +
            "\nID del objeto: " + gameObject.GetInstanceID() +
            "\nEscena del objeto: " + gameObject.scene.name +
            "\nInstancia principal: " + (Instance == this) +
            "\nNetworkSession principal: " +
            (NetworkSession.Instance != null
                ? NetworkSession.Instance.GetInstanceID().ToString()
                : "NULL")
        );

        if (Instance == this)
            Instance = null;
    }
}
