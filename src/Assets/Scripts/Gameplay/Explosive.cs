using Unity.Netcode;
using UnityEngine;

public enum BombType
{
    Physical,
    E4,
}

public class Explosive : NetworkBehaviour
{
    [Header("Settings")]
    public BombType type = BombType.Physical;
    public float fuseTime = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 10f;
    public bool explodeOnContact = false;
    public bool autoExplode = false;
    public bool destroyOnExplode = true;
    public float destroyDelay = 0.5f;

    // Event for other systems to react
    public System.Action<Vector2, float, float, GameObject> OnExplode;

    private bool hasExploded = false;
    private bool fuseActive = false;
    private float fuseTimer = 0f;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (autoExplode)
            StartFuse();
    }

    private void Update()
    {
        if (IsOwner && Input.GetKeyDown(KeyCode.F))
        {
            StartFuseServerRpc();
        }

        if (!IsServer)
            return;
        if (!fuseActive || hasExploded)
            return;

        fuseTimer += Time.deltaTime;

        // Start blinking in the last second
        if (sr != null && fuseTimer >= fuseTime - 1f)
        {
            float blink = Mathf.PingPong(Time.time * 10f, 1f);
            sr.enabled = blink > 0.5f;
        }

        if (fuseTimer >= fuseTime)
            ExplodeServerSide();
    }

    private void OnCollisionEnter2D(Collision2D _)
    {
        if (!IsServer || !explodeOnContact || hasExploded)
            return;
        ExplodeServerSide();
    }

    private void OnTriggerEnter2D(Collider2D _)
    {
        if (!IsServer || !explodeOnContact || hasExploded)
            return;
        ExplodeServerSide();
    }

    public void StartFuse()
    {
        if (!IsServer || fuseActive || hasExploded)
            return;
        fuseActive = true;
        fuseTimer = 0f;
    }

    public void ExplodeNow()
    {
        if (!IsServer)
            return;
        ExplodeServerSide();
    }

    private void ExplodeServerSide()
    {
        if (hasExploded)
            return;
        hasExploded = true;

        OnExplode?.Invoke((Vector2)transform.position, explosionRadius, explosionForce, gameObject);

        // Chain reaction
        foreach (
            var hit in Physics2D.OverlapCircleAll((Vector2)transform.position, explosionRadius)
        )
            if (hit.TryGetComponent<Explosive>(out var other) && other != this)
                other.StartFuse();

        if (destroyOnExplode)
            Invoke(nameof(DespawnDelayed), destroyDelay);
    }

    [ServerRpc]
    private void StartFuseServerRpc()
    {
        StartFuse();
    }

    private void DespawnDelayed()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn();
        else
            Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.3f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
