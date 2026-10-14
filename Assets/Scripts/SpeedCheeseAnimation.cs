using UnityEngine;

public class SpeedCheeseAnimation : MonoBehaviour
{
    [Header("Objeto visual que se anima")]
    [SerializeField] private Transform visualRoot;

    [Header("Velocidad de giro")]
    [SerializeField] private float rotationSpeed = 100f;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;
    }

    private void Update()
    {
        visualRoot.Rotate(
            0f,
            rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }
}