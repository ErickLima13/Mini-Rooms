using R3;                     // R3
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed = 2f;

    private InputSystem_Actions inputActions; // Sua classe gerada pelo New Input System
    private Vector2 moveInput;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Start()
    {
        // 1. New Input System captura a intenção de movimento
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // 2. R3 assume o controle do loop de Física (FixedUpdate reativo)
        Observable.EveryUpdate(UnityFrameProvider.FixedUpdate)
            .Subscribe(_ =>
            {
                // Aplica a movimentação Top-Down no Rigidbody
                rb.linearVelocity = moveInput * speed;

                if (moveInput.x > 0.1f)
                {
                    transform.localScale = Vector3.one; // Olhando para a direita (1, 1, 1)
                }

                else if (moveInput.x < -0.1f)
                {
                    transform.localScale = new Vector3(-1f, 1f, 1f); // Olhando para a esquerda (-1, 1, 1)
                }

            })
            .RegisterTo(this.destroyCancellationToken); // Cancela ao destruir o objeto
    }
}
