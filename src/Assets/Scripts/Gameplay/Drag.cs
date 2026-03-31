using Unity.Netcode;
using UnityEngine;

public class Drag : NetworkBehaviour
{
    [Header("Dragging")]
    public float dragLerpSpeed = 18f;
    public float throwMultiplier = 35f;

    private Rigidbody2D rb;
    private Camera mainCam;
    private bool isBeingDragged = false;
    private Vector3 dragOffset;
    private Vector3 previousPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    private void OnMouseDown()
    {
        if (!IsOwner)
            RequestOwnershipServerRpc();
        dragOffset = transform.position - mainCam.ScreenToWorldPoint(Input.mousePosition);
        previousPosition = transform.position;
        isBeingDragged = true;
        BeginDragServerRpc();
    }

    private void OnMouseDrag()
    {
        if (!isBeingDragged)
            return;
        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 target = mouseWorld + (Vector2)dragOffset;
        previousPosition = transform.position;
        MoveServerRpc(target);
    }

    private void OnMouseUp()
    {
        if (!isBeingDragged)
            return;
        isBeingDragged = false;
        Vector2 vel = ((Vector2)transform.position - (Vector2)previousPosition) / Time.deltaTime;
        if (vel.magnitude > 40f)
            vel = vel.normalized * 40f;
        EndDragServerRpc(vel * throwMultiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams p = default)
    {
        GetComponent<NetworkObject>().ChangeOwnership(p.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void BeginDragServerRpc()
    {
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveServerRpc(Vector2 target)
    {
        rb.MovePosition(
            Vector2.Lerp(transform.position, target, dragLerpSpeed * Time.fixedDeltaTime)
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndDragServerRpc(Vector2 velocity)
    {
        rb.isKinematic = false;
        rb.velocity = velocity;
    }
}
