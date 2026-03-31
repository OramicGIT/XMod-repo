using Unity.Netcode; // 1. Added namespace
using UnityEngine;

public class Pistol : NetworkBehaviour // 2. Changed from MonoBehaviour
{
    [Header("Setup")]
    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private GameObject bulletPrefab;
    private Camera mainCamera;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 3. Only the owner of this pistol should process input
        if (!IsOwner)
            return;

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (col != null && col.OverlapPoint(mouseWorld))
        {
            // 4. Request the server to fire
            FireServerRpc();
        }
    }

    // 5. ServerRpc runs on the Server, called by the Client
    [ServerRpc]
    private void FireServerRpc()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // 6. Spawn the object across the network
        bullet.GetComponent<NetworkObject>().Spawn();
    }
}
