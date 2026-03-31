using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CharacterControl2D : NetworkBehaviour
{
    public enum MovementType
    {
        Grounded,
        Flying,
        Hovercar,
    }

    [Header("Movement Settings")]
    [SerializeField]
    private MovementType movementType = MovementType.Grounded;

    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float jumpForce = 8f;

    [Header("AI Settings")]
    [SerializeField]
    private bool noAI = false;

    [SerializeField]
    private float aiChangeTime = 2f;

    [SerializeField]
    private float aiJumpChance = 0.4f;

    [Header("Ground Check")]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private float checkRadius = 0.2f;

    [SerializeField]
    private Vector2 groundCheckOffset = new Vector2(0, -0.5f);

    private Rigidbody2D rb;
    private Collider2D col;
    private Camera mainCam;
    private SpriteRenderer sr;

    private float lastGroundedY;
    private const float HoverMaxHeight = 11f;

    public NetworkVariable<bool> isPlayerControlled = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Vector2 clientInput;
    private bool clientJumpRequested;

    private Vector2 aiInput;
    private bool aiJumpQueued;

    private static readonly NetworkVariable<bool> allStopped = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        rb.freezeRotation = true;
        ConfigurePhysics();
    }

    private void ConfigurePhysics()
    {
        if (movementType == MovementType.Grounded)
        {
            rb.gravityScale = 2f;
            rb.drag = 0f;
        }
        else
        {
            rb.gravityScale = 0f;
            rb.drag = 5f;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !noAI)
            InvokeRepeating(nameof(UpdateAIDecisions), 1f, aiChangeTime);
    }

    void Update()
    {
        if (!IsClient || isPlayerControlled.Value == false)
            return;

        ReadPlayerInput();
        HandleSelectionInput();
    }

    private void ReadPlayerInput()
    {
        float h = Input.GetAxisRaw("Aihoz");
        float v = Input.GetAxisRaw("Aivert");
        bool jump = Input.GetKey(KeyCode.Space);

        UpdateInputServerRpc(h, v, jump);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateInputServerRpc(float horizontal, float vertical, bool jump)
    {
        clientInput = new Vector2(horizontal, vertical);
        if (movementType == MovementType.Grounded && jump && IsGrounded())
            clientJumpRequested = true;
    }

    void FixedUpdate()
    {
        if (!IsServer)
            return;

        Vector2 velocity = rb.velocity;
        bool grounded = IsGrounded();
        if (grounded)
            lastGroundedY = transform.position.y;

        velocity.y = CalculateVerticalVelocity(grounded);
        velocity.x = CalculateHorizontalVelocity();

        rb.velocity = velocity;

        UpdateSpriteDirection(velocity.x);
    }

    private float CalculateHorizontalVelocity()
    {
        if (isPlayerControlled.Value)
            return clientInput.x * moveSpeed;
        if (allStopped.Value || noAI)
            return 0f;
        return aiInput.x * moveSpeed;
    }

    private float CalculateVerticalVelocity(bool grounded)
    {
        switch (movementType)
        {
            case MovementType.Grounded:
                if ((isPlayerControlled.Value && clientJumpRequested) || aiJumpQueued)
                {
                    if (grounded)
                    {
                        clientJumpRequested = false;
                        aiJumpQueued = false;
                        return jumpForce;
                    }
                }
                return rb.velocity.y;

            case MovementType.Flying:
                return (isPlayerControlled.Value ? clientInput.y : aiInput.y) * moveSpeed;

            case MovementType.Hovercar:
                float targetY = (isPlayerControlled.Value ? clientInput.y : aiInput.y) * moveSpeed;
                float ceiling = lastGroundedY + HoverMaxHeight;
                if (transform.position.y >= ceiling && targetY > 0)
                    return 0f;
                return targetY;

            default:
                return rb.velocity.y;
        }
    }

    private void UpdateSpriteDirection(float horizontal)
    {
        if (sr != null && Mathf.Abs(horizontal) > 0.01f)
            sr.flipX = horizontal < 0f;
    }

    private void HandleSelectionInput()
    {
        if (isPlayerControlled.Value)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        if (!col.OverlapPoint(mousePos))
            return;

        RequestControlServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestControlServerRpc()
    {
        foreach (var c in FindObjectsOfType<CharacterControl2D>())
            c.isPlayerControlled.Value = false;

        isPlayerControlled.Value = true;
    }

    private void UpdateAIDecisions()
    {
        if (isPlayerControlled.Value || allStopped.Value || noAI)
            return;

        aiInput.x = UnityEngine.Random.Range(-1, 2);
        aiInput.y = (movementType == MovementType.Grounded) ? 0f : UnityEngine.Random.Range(-1, 2);

        if (movementType == MovementType.Grounded && UnityEngine.Random.value < aiJumpChance)
            aiJumpQueued = true;
    }

    private bool IsGrounded()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            (Vector2)transform.position + groundCheckOffset,
            checkRadius
        );
        foreach (var hit in hits)
            if (hit != col && !hit.isTrigger)
                return true;

        return false;
    }
}
