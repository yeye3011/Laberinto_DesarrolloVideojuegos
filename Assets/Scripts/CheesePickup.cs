using UnityEngine;

public class CheesePickup : MonoBehaviour
{
    private NetworkCheeseIdentity cheeseIdentity;

    private bool alreadyRequested;

    private void Awake()
    {
        FindIdentity();
    }

    private void Start()
    {
        FindIdentity();
    }

    private void FindIdentity()
    {
        if (cheeseIdentity != null)
            return;

        cheeseIdentity = GetComponent<NetworkCheeseIdentity>();

        if (cheeseIdentity == null)
        {
            Debug.LogWarning(
                "CheesePickup: todavía no encuentro NetworkCheeseIdentity en "
                + gameObject.name
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "TRIGGER: "
            + gameObject.name
            + " tocó a "
            + other.name
        );

        if (alreadyRequested)
            return;

        FindIdentity();

        if (cheeseIdentity == null)
        {
            Debug.LogError(
                "CheesePickup: no existe NetworkCheeseIdentity en "
                + gameObject.name
            );

            return;
        }

        PlayerIdentity playerIdentity = other.GetComponentInParent<PlayerIdentity>();

        if (playerIdentity == null)
        {
            Debug.Log("TRIGGER: el objeto que tocó el queso no pertenece a un Player.");
            return;
        }

        if (NetworkSession.Instance == null)
        {
            Debug.LogError("No existe NetworkSession.");
            return;
        }

        int localPlayerId = NetworkSession.Instance.LocalPlayerId;

        if (playerIdentity.PlayerId != localPlayerId)
        {
            return;
        }

        alreadyRequested = true;

        int cheeseId = cheeseIdentity.CheeseId;
        int playerId = playerIdentity.PlayerId;

        Debug.Log("PICKUP: jugador " + playerId + " tocó queso ID " + cheeseId);

        if (AudioManager.Instance != null)
        {
            if (cheeseIdentity.CheeseType == "Speed")
            {
                AudioManager.Instance.ReproducirEfecto(AudioManager.Instance.PowerupVelocidad);
            }
            else if (cheeseIdentity.CheeseType == "Normal") 
            {
                AudioManager.Instance.ReproducirEfecto(AudioManager.Instance.QuesoNormal);
            }
        }

        NetworkCheeseManager manager = FindFirstObjectByType<NetworkCheeseManager>();

        if (manager != null)
        {
            manager.RequestCheeseCollection(cheeseId, playerId);
        }
    }
}