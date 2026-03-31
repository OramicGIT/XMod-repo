using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CameraCC : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float zoomSmoothTime = 0.1f;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float acceleration = 10f; // how fast velocity changes
    public float drag = 5f; // how quickly velocity slows down

    private Camera mainCamera;
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private Vector3 startPosition;

    private float zoomVelocity = 0f;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        startPosition = transform.position;

        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Initialize collider size
        if (coll != null)
        {
            float height = mainCamera.orthographicSize * 2;
            float width = height * mainCamera.aspect;
            coll.size = new Vector2(width, height);
        }
    }

    void Update()
    {
        HandleCameraZoom();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            rb.position = startPosition;
            rb.velocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        // Get input
        float h = Input.GetAxisRaw("Camhoz");
        float v = Input.GetAxisRaw("Camvert");
        Vector2 input = new Vector2(h, v).normalized;
        float zoomModifier = mainCamera.orthographicSize / 10f;
        Vector2 targetVelocity = input * moveSpeed * zoomModifier;
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        if (input.magnitude < 0.01f)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, drag * Time.fixedDeltaTime);
        }
    }

    private void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float targetZoom = Mathf.Clamp(
                mainCamera.orthographicSize - scroll * zoomSpeed * mainCamera.orthographicSize,
                minZoom,
                maxZoom
            );
            mainCamera.orthographicSize = Mathf.SmoothDamp(
                mainCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime
            );
        }
    }
}
