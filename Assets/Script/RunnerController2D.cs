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

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Detectar si está tocando el suelo
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (anim != null)
            anim.SetBool("IsJumping", !isGrounded);
    }

    public void Jump()
    {
        if (!isGrounded) return;

        // Reset de velocidad vertical para que el salto sea consistente
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (anim != null)
            anim.SetBool("IsJumping", true);
    }

    // Para ver el circulito en la escena
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}