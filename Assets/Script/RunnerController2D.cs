using UnityEngine;

public class RunnerController2D : MonoBehaviour
{
    [Header("Jump")]
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    Rigidbody2D rb;
    bool isGrounded;
    Animator anim;
    Collider2D col;

    bool gameStarted = false;

    void Start()
    {
        SetGameStarted(false);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (!gameStarted) return;
        // Detectar si está tocando el suelo
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (anim != null)
            anim.SetBool("IsJumping", !isGrounded);
    }

    public void Jump(float multiplier = 1f)
    {
        if (!isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce * multiplier, ForceMode2D.Impulse);
    }

    // Para ver el circulito en la escena
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
    public void SetGameStarted(bool started)
    {
        gameStarted = started;

        if (rb != null)
            rb.simulated = started;

        if (anim != null)
            anim.enabled = started;
    }
}