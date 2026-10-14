using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 20f;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Animación")]
    [SerializeField] private Animator animator;

    private CharacterController characterController;

    private float velocidadOriginal;
    private Coroutine corrutinaVelocidad;

    // Indica si este jugador puede recibir el joystick local.
    private bool isLocalPlayer = true;
    private bool isFrozen = false;
    public bool IsLocalPlayer => isLocalPlayer;
    public int CurrentState { get; private set; }

    private void Awake()
    {
        velocidadOriginal = moveSpeed;

        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    public void SetLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
    }

    private void Update()
    {
        // Los jugadores remotos NO reciben el joystick de este dispositivo.
        if (!isLocalPlayer) return;

        if (isFrozen)
        {
            CurrentState = 0;

            if (animator != null)
                animator.SetBool("IsMoving", false);

            return;
        }

        if (moveAction == null) return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        input = Vector2.ClampMagnitude(input, 1f);

        // -------------------------
        // ANIMACIÓN
        // -------------------------

        bool isMoving = input.magnitude > 0.1f;

        CurrentState = isMoving ? 1 : 0;

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // -------------------------
        // ROTACIÓN
        // -------------------------

        float rotation = input.x * rotationSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up, rotation);

        // -------------------------
        // MOVIMIENTO
        // -------------------------

        float movement = input.y;

        Vector3 direction = transform.forward * movement;

        if (characterController != null)
        {
            characterController.Move(
                direction * moveSpeed * Time.deltaTime
            );
        }
    }


    public void ActivarVelocidad(float multiplicador, float duracion)
    {
        if (corrutinaVelocidad != null)
        {
            StopCoroutine(corrutinaVelocidad);
        }

        corrutinaVelocidad = StartCoroutine(
            EfectoVelocidad(multiplicador, duracion)
        );
    }

    private System.Collections.IEnumerator EfectoVelocidad(
        float multiplicador,
        float duracion)
    {
        moveSpeed = velocidadOriginal * multiplicador;

        yield return new WaitForSeconds(duracion);

        moveSpeed = velocidadOriginal;
        corrutinaVelocidad = null;
    }

    public void DesactivarVelocidad()
    {
        if (corrutinaVelocidad != null)
        {
            StopCoroutine(corrutinaVelocidad);
            corrutinaVelocidad = null;
        }

        moveSpeed = velocidadOriginal;
    }

    public void CongelarMovimiento()
    {
        isFrozen = true;
    }

    public void DescongelarMovimiento()
    {
        isFrozen = false;
    }
}