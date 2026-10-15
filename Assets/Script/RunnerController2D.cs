using UnityEngine;

public class RunnerController2D : MonoBehaviour
{
    [Header("Jump")]
    public float jumpForce = 10f;
    [Range(0.75f, 1.25f)] public float risingGravityMultiplier = 0.9f;
    [Range(0.75f, 1.5f)] public float fallingGravityMultiplier = 1.05f;
    public float obstacleClearanceMargin = 0.12f;
    [Range(0f, 0.4f)] public float preferredClearPhase = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Hit Response")]
    public float invulnerabilitySeconds = 1.2f;
    public float blinkInterval = 0.08f;

    Rigidbody2D rb;
    bool isGrounded;
    Animator anim;
    SpriteRenderer spriteRenderer;
    SpriteRenderer duckRenderer;
    Collider2D bodyCollider;
    CapsuleCollider2D capsuleCollider;
    Vector2 standingColliderSize;
    Vector2 standingColliderOffset;
    float duckUntil;
    float baseGravityScale;
    float invulnerabilityTimer;
    float blinkTimer;
    bool blinkVisible = true;

    bool gameStarted = false;

    public bool IsGrounded => isGrounded;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;
    public bool IsDucking => duckUntil > Time.time;

    void Start()
    {
        SetGameStarted(false);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            GameObject visual = new GameObject("DuckVisual");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = new Vector3(1f, 0.52f, 1f);
            visual.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            duckRenderer = visual.AddComponent<SpriteRenderer>();
            duckRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            duckRenderer.sortingOrder = spriteRenderer.sortingOrder;
            visual.SetActive(false);
        }
        bodyCollider = GetComponent<Collider2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null)
        {
            standingColliderSize = capsuleCollider.size;
            standingColliderOffset = capsuleCollider.offset;
        }

        if (rb != null)
            baseGravityScale = rb.gravityScale;
    }

    void Update()
    {
        UpdateGroundedState();
        UpdateGravityScale();
        UpdateInvulnerabilityVisual();
        if (duckUntil > 0f && Time.time >= duckUntil)
            RestoreStandingCollider();

        if (!gameStarted) return;

        // Detectar si est� tocando el suelo
        if (anim != null)
            anim.SetBool("IsJumping", !isGrounded);
    }

    public void Jump(float multiplier = 1f)
    {
        if (!isGrounded || IsDucking || rb == null) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * multiplier);
        isGrounded = false;
    }

    public void Duck(float seconds = 1.25f)
    {
        if (!gameStarted || !isGrounded || capsuleCollider == null)
            return;

        duckUntil = Mathf.Max(duckUntil, Time.time + seconds);
        capsuleCollider.size = new Vector2(standingColliderSize.x, standingColliderSize.y * 0.52f);
        capsuleCollider.offset = standingColliderOffset - Vector2.up * (standingColliderSize.y * 0.24f);
        if (duckRenderer != null)
        {
            duckRenderer.sprite = spriteRenderer.sprite;
            duckRenderer.color = spriteRenderer.color;
            duckRenderer.gameObject.SetActive(true);
            spriteRenderer.enabled = false;
        }
    }

    private void RestoreStandingCollider()
    {
        duckUntil = 0f;
        if (capsuleCollider != null)
        {
            capsuleCollider.size = standingColliderSize;
            capsuleCollider.offset = standingColliderOffset;
        }
        if (duckRenderer != null)
        {
            duckRenderer.gameObject.SetActive(false);
            spriteRenderer.enabled = true;
        }
    }

    public void TriggerHitInvulnerability()
    {
        invulnerabilityTimer = invulnerabilitySeconds;
        blinkTimer = 0f;
        blinkVisible = false;
        ApplyBlinkState();
    }

    public float GetRecommendedObstacleContactDelay(float obstacleTopY, float jumpMultiplier = 1f)
    {
        float worldGravity = Mathf.Abs(Physics2D.gravity.y);
        float upwardsGravity = Mathf.Max(0.01f, baseGravityScale * worldGravity * risingGravityMultiplier);
        float downwardsGravity = Mathf.Max(0.01f, baseGravityScale * worldGravity * fallingGravityMultiplier);
        float initialVelocity = jumpForce * jumpMultiplier;

        float runnerBottomY = GetRunnerBottomY();
        float requiredHeight = obstacleTopY + obstacleClearanceMargin - runnerBottomY;

        float apexTime = initialVelocity / upwardsGravity;
        float apexHeight = (initialVelocity * apexTime) - (0.5f * upwardsGravity * apexTime * apexTime);

        if (requiredHeight <= 0f)
            return Mathf.Clamp(apexTime * 0.72f, 0.16f, 0.42f);

        if (apexHeight <= requiredHeight)
            return Mathf.Clamp(apexTime * 0.9f, 0.18f, 0.46f);

        float ascentCrossTime = (initialVelocity - Mathf.Sqrt(Mathf.Max(0f, (initialVelocity * initialVelocity) - (2f * upwardsGravity * requiredHeight)))) / upwardsGravity;
        float descentCrossTime = apexTime + Mathf.Sqrt(Mathf.Max(0f, (2f * (apexHeight - requiredHeight)) / downwardsGravity));
        float safeWindow = Mathf.Max(0.02f, descentCrossTime - ascentCrossTime);
        float preferredTime = ascentCrossTime + (safeWindow * preferredClearPhase);

        return Mathf.Clamp(preferredTime, 0.16f, 0.52f);
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
        {
            rb.simulated = started;
            rb.gravityScale = baseGravityScale;
            if (!started)
                rb.linearVelocity = Vector2.zero;
        }

        if (anim != null)
            anim.enabled = started;

        if (!started)
        {
            RestoreStandingCollider();
            isGrounded = false;
            invulnerabilityTimer = 0f;
            blinkVisible = true;
            ApplyBlinkState();
        }
    }

    void UpdateGroundedState()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void UpdateGravityScale()
    {
        if (rb == null)
            return;

        if (!gameStarted)
        {
            rb.gravityScale = baseGravityScale;
            return;
        }

        if (isGrounded)
        {
            rb.gravityScale = baseGravityScale;
            return;
        }

        float gravityMultiplier = rb.linearVelocity.y >= 0f
            ? risingGravityMultiplier
            : fallingGravityMultiplier;

        rb.gravityScale = baseGravityScale * gravityMultiplier;
    }

    void UpdateInvulnerabilityVisual()
    {
        if (invulnerabilityTimer <= 0f)
        {
            if (!blinkVisible)
            {
                blinkVisible = true;
                ApplyBlinkState();
            }

            return;
        }

        invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
        blinkTimer -= Time.deltaTime;

        if (blinkTimer > 0f)
            return;

        blinkTimer = blinkInterval;
        blinkVisible = !blinkVisible;
        ApplyBlinkState();
    }

    void ApplyBlinkState()
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = blinkVisible ? 1f : 0.35f;
        spriteRenderer.color = color;
        if (duckRenderer != null) duckRenderer.color = color;
    }

    float GetRunnerBottomY()
    {
        if (bodyCollider != null)
            return bodyCollider.bounds.min.y;

        return transform.position.y - 0.75f;
    }
}
