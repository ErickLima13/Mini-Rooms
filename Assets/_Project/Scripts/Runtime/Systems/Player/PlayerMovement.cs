using R3;                     // R3
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações do Pulo")]
    public float forcaPulo;
    public Transform verificadorChao;
    public LayerMask camadaChao;
    public float raioVerificacao;
    private bool estaNoChao;
    private bool querPular; // Controle seguro para o frame de física

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed;

    [SerializeField] private Animator animator;

    private InputSystem_Actions inputActions; // Sua classe gerada pelo New Input System
    private Vector2 moveInput;

    private float speedY;

    public int Point { get; set; }

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Start()
    {
        // 1. New Input System captura a intenção de movimento
        inputActions.Player.Move.performed += ctx =>
        {
            var input = ctx.ReadValue<Vector2>();
            moveInput = new Vector2(input.x, 0f);
        };
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // 2. Captura o clique do Pulo e ativa a intenção (Sinal limpo)
        inputActions.Player.Jump.performed += ctx =>
        {
            if (estaNoChao)
            {
                querPular = true;
                animator.SetBool("jump", true);
            }
        };

        // 3. R3 assume o controle do loop de Física (FixedUpdate reativo)
        Observable.EveryUpdate(UnityFrameProvider.FixedUpdate)
            .Subscribe(_ =>
            {
                speedY = rb.linearVelocityY;

                // Verifica o chão
                estaNoChao = Physics2D.OverlapCircle(verificadorChao.position, raioVerificacao, camadaChao);

                // Define a velocidade horizontal e mantém a gravidade nativa no Y
                float velocidadeX = moveInput.x * speed;
                float velocidadeY = rb.linearVelocity.y;

                // Aplica a força do pulo caso o jogador tenha apertado o botão
                if (querPular)
                {
                    
                    rb.AddForce(new Vector2(0, forcaPulo));

                    //velocidadeY = forcaPulo;
                    querPular = false; // Reseta imediatamente para ele poder cair e não pular de novo sozinho
                }

                if (estaNoChao && Mathf.Abs(moveInput.x) < 0.01f)
                {
                    velocidadeX = 0f; // Zera o movimento horizontal para o player travar no lugar

                }


                // Aplica a física final ao Rigidbody de forma segura
                rb.linearVelocity = new Vector2(velocidadeX, velocidadeY);

                Animations();

                Flip();
            })
            .RegisterTo(this.destroyCancellationToken); // Cancela ao destruir o objeto
    }

    private void Animations()
    {
        animator.SetFloat("yVelocity", speedY);
        animator.SetFloat("xVelocity", Mathf.Abs(moveInput.x));
        animator.SetBool("isGrounded", estaNoChao);
        animator.SetBool("jump", !estaNoChao);
    }

    private void Flip()
    {
        // Lógicas de Flip (Olhar para os lados)
        if (moveInput.x > 0.1f)
        {
            transform.localScale = Vector3.one;
        }
        else if (moveInput.x < -0.1f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (verificadorChao != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(verificadorChao.position, raioVerificacao);
        }
    }
}
