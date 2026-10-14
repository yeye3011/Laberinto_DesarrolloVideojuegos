using UnityEngine;
using UnityEngine.EventSystems;

public class CameraTouchController :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Sensibilidad")]
    [SerializeField] private float rotationSensitivity = 0.8f;

    [Header("Límite por gesto")]
    [SerializeField] private float maxRotationPerGesture = 90f;

    private PlayerMovement localPlayer;

    private bool isDragging;
    private float accumulatedRotation;

    private void Start()
    {
        BuscarJugadorLocal();
    }

    private void BuscarJugadorLocal()
    {
        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (PlayerMovement player in players)
        {
            if (player.IsLocalPlayer)
            {
                localPlayer = player;
                break;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (localPlayer == null)
        {
            BuscarJugadorLocal();
        }

        if (localPlayer == null)
            return;

        isDragging = true;
        accumulatedRotation = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (localPlayer == null)
            return;

        float rotation =
            eventData.delta.x *
            rotationSensitivity;

        float remainingRotation =
            maxRotationPerGesture -
            Mathf.Abs(accumulatedRotation);

        if (remainingRotation <= 0f)
            return;

        rotation =
            Mathf.Clamp(
                rotation,
                -remainingRotation,
                remainingRotation
            );

        localPlayer.transform.Rotate(
            Vector3.up,
            rotation
        );

        accumulatedRotation += rotation;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        accumulatedRotation = 0f;
    }
}
