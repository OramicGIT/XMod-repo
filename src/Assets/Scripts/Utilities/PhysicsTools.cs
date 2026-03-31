using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum JointType
{
    Weld,
    Rope,
}

public class PhysicsTools : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector3 duplicateOffset = new Vector3(0.5f, 0.5f, 0);
    private UISpawn uiSpawn;

    private void Awake()
    {
        uiSpawn = FindObjectOfType<UISpawn>();
    }

    // --- JOINTS ---
    public void WeldSelected() => CreateJointsGroup(JointType.Weld);

    public void RopeSelected() => CreateJointsGroup(JointType.Rope);

    private void CreateJointsGroup(JointType type)
    {
        if (SBox.selected.Count < 2)
            return;

        var selectedObjects = SBox
            .selected.Where(d => d != null)
            .Select(d => d.gameObject)
            .ToArray();

        if (selectedObjects.Length >= 2)
            CreateJoints(selectedObjects, type);
    }

    private void CreateJoints(GameObject[] objects, JointType type)
    {
        for (int i = 0; i < objects.Length - 1; i++)
        {
            ApplyJoint(objects[i], objects[i + 1], type);
        }
    }

    private void ApplyJoint(GameObject a, GameObject b, JointType type)
    {
        if (
            !a.TryGetComponent<Rigidbody2D>(out var rbA)
            || !b.TryGetComponent<Rigidbody2D>(out var rbB)
        )
            return;

        switch (type)
        {
            case JointType.Weld:
                var weld = a.AddComponent<FixedJoint2D>();
                weld.connectedBody = rbB;
                break;

            case JointType.Rope:
                var rope = a.AddComponent<DistanceJoint2D>();
                rope.connectedBody = rbB;
                rope.distance = Vector2.Distance(a.transform.position, b.transform.position);
                rope.maxDistanceOnly = true;
                break;
        }
    }

    // --- FREEZE ---
    public void ToggleFreeze()
    {
        if (SBox.selected.Count == 0)
            return;

        bool freeze = !IsGroupFrozen();
        foreach (var drag in SBox.selected)
        {
            if (drag != null)
            {
                var rb = drag.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = freeze;
                    rb.constraints = freeze
                        ? RigidbodyConstraints2D.FreezeAll
                        : RigidbodyConstraints2D.None;
                    if (freeze)
                    {
                        rb.velocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                }
            }
        }
    }

    private bool IsGroupFrozen()
    {
        if (SBox.selected.Count == 0)
            return false;
        var first = SBox.selected[0];
        if (first == null)
            return false;

        if (first.TryGetComponent<Rigidbody2D>(out var rb2d))
            return rb2d.isKinematic;
        return false;
    }

    // --- DELETE ---
    public void DeleteSelected()
    {
        if (SBox.selected.Count == 0)
            return;

        foreach (var drag in SBox.selected.ToArray())
        {
            if (drag != null)
                continue;
            var netObj = drag.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Destroy(drag.gameObject);
        }

        SBox.selected.Clear();
    }

    public void DuplicateSelected()
    {
        if (SBox.selected.Count == 0)
            return;

        foreach (var drag in SBox.selected)
        {
            if (drag == null)
                continue;
            Vector3 pos = drag.transform.position + duplicateOffset;
            var nmo = drag.GetComponent<NetworkedModObject>();

            if (nmo != null)
                uiSpawn.SpawnModServerRpc(nmo.GetActiveModID(), pos);
            else
                uiSpawn.SpawnOfficialServerRpc(pos, (byte)drag.GetComponent<LevelBlockInfo>().type);
        }
    }

    // --- IGNITE ---
    public void IgniteSelected()
    {
        if (SBox.selected.Count == 0)
            return;

        foreach (var drag in SBox.selected)
        {
            if (drag == null)
                continue;
            if (drag.TryGetComponent<FireManager>(out var fire))
                fire.Ignite();
        }
    }
}
