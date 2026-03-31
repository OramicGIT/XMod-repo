using Unity.Netcode;
using UnityEngine;

public class IcePrank : NetworkBehaviour
{
    [Header("April Fools Settings")]
    [SerializeField]
    private PhysicsMaterial2D iceMaterial; // Friction = 0

    private Collider2D col;
    private PhysicsMaterial2D originalMaterial;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
            originalMaterial = col.sharedMaterial;
    }

    public override void OnNetworkSpawn()
    {
        ApplyAprilFoolsPrank();
    }

    private void ApplyAprilFoolsPrank()
    {
        if (col == null)
            return;

        bool isAprilFools = System.DateTime.Now.Month == 4 && System.DateTime.Now.Day == 1;

        col.sharedMaterial = isAprilFools ? iceMaterial : originalMaterial;

        if (isAprilFools && TryGetComponent<Rigidbody2D>(out var rb))
            rb.WakeUp();
    }
}
