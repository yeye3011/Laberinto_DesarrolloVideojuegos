using UnityEngine;

public class IceCheeseAnimation : MonoBehaviour
{
    [Header("Objeto visual que se anima")]
    [SerializeField] private Transform visualRoot;

    [Header("Animación")]
    [SerializeField] private float floatAmplitude = 1.2f;
    [SerializeField] private float floatSpeed = 3f;

    private Vector3 initialLocalPosition;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        initialLocalPosition = visualRoot.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        visualRoot.localPosition =
            initialLocalPosition + Vector3.up * offset;
    }
}