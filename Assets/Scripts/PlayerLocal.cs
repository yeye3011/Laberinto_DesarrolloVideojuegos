using UnityEngine;

public class PlayerLocal : MonoBehaviour
{
    [Header("Cámara del jugador")]
    [SerializeField] private Camera playerCamera;

    [Header("Audio")]
    [SerializeField] private AudioListener audioListener;

    [Header("Movimiento")]
    [SerializeField] private PlayerMovement playerMovement;

    public void SetLocalPlayer(bool isLocal)
    {
        // Cámara
        if (playerCamera != null)
            playerCamera.enabled = isLocal;

        // Audio Listener
        if (audioListener != null)
            audioListener.enabled = isLocal;

        // Control del joystick
        if (playerMovement != null)
            playerMovement.SetLocalPlayer(isLocal);
    }
}