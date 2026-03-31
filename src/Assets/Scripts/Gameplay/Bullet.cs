using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField]
    private float speed = 20f;

    [SerializeField]
    private float pushForce = 10f;

    [SerializeField]
    private float lifetime = 3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        rb.velocity = transform.right * speed;
        Invoke(nameof(DespawnBullet), lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer)
            return;

        Rigidbody2D hitRb = collision.rigidbody;

        if (hitRb != null && !hitRb.isKinematic)
        {
            Vector2 dir = (collision.relativeVelocity * -1).normalized;
            hitRb.AddForce(dir * pushForce, ForceMode2D.Impulse);
        }

        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
