using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class SaveData
{
    public List<ObjectData> objects = new();
}

[Serializable]
public class ObjectData
{
    public string modID;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public ulong networkID;
    public List<JointData> joints = new();
}

[Serializable]
public class JointData
{
    public ulong connectedNetworkId;
    public string jointType;
}

public class Save : NetworkBehaviour
{
    private string SavePath => Path.Combine(Application.persistentDataPath, "world.json");

    public void SaveWorld()
    {
        if (!IsServer)
            return;
        if (SBox.selected.Count == 0)
            return;

        var data = new SaveData();

        foreach (var modObj in GetNetworkedFromSelected())
        {
            if (modObj == null)
                continue;
            var netObj = modObj.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            var objData = new ObjectData
            {
                modID = modObj.GetActiveModID(),
                position = modObj.transform.position,
                rotation = modObj.transform.rotation,
                scale = modObj.transform.localScale,
                networkID = netObj.NetworkObjectId,
            };

            foreach (var joint in modObj.GetComponents<Joint2D>())
            {
                if (joint.connectedBody == null)
                    continue;
                var targetNet = joint.connectedBody.GetComponent<NetworkObject>();
                if (targetNet == null)
                    continue;
                objData.joints.Add(
                    new JointData
                    {
                        connectedNetworkId = targetNet.NetworkObjectId,
                        jointType = joint is FixedJoint2D ? "Weld" : "Rope",
                    }
                );
            }

            data.objects.Add(objData);
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    private List<NetworkedModObject> GetNetworkedFromSelected()
    {
        var result = new List<NetworkedModObject>();
        foreach (var drag in SBox.selected)
        {
            if (drag == null)
                continue;
            var mod = drag.GetComponent<NetworkedModObject>();
            if (mod != null)
                result.Add(mod);
        }
        return result;
    }

    public void TriggerLoadWorld()
    {
        if (!IsServer)
            return;
        if (!File.Exists(SavePath))
            return;

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        ClearWorld();
        SpawnObjects(data);
    }

    private void ClearWorld()
    {
        foreach (var obj in FindObjectsOfType<NetworkedModObject>())
        {
            var net = obj.GetComponent<NetworkObject>();
            if (net != null && net.IsSpawned)
                net.Despawn();
            else
                Destroy(obj.gameObject);
        }
    }

    private void SpawnObjects(SaveData data)
    {
        var idMap = new Dictionary<ulong, NetworkObject>();

        foreach (var obj in data.objects)
        {
            var go = Instantiate(
                ModManager.Instance.networkedModPrefab,
                obj.position,
                obj.rotation
            );
            go.transform.localScale = obj.scale;
            var net = go.GetComponent<NetworkObject>();
            if (net != null)
                net.Spawn();
            go.GetComponent<NetworkedModObject>().SetModID(obj.modID);
            idMap[obj.networkID] = net;
        }

        foreach (var obj in data.objects)
        {
            if (obj.joints.Count == 0 || !idMap.TryGetValue(obj.networkID, out var sourceNet))
                continue;
            foreach (var joint in obj.joints)
            {
                if (!idMap.TryGetValue(joint.connectedNetworkId, out var targetNet))
                    continue;
                ApplyJoint(sourceNet.gameObject, targetNet.gameObject, joint.jointType);
            }
        }
    }

    private void ApplyJoint(GameObject a, GameObject b, string type)
    {
        if (
            !a.TryGetComponent<Rigidbody2D>(out var rbA)
            || !b.TryGetComponent<Rigidbody2D>(out var rbB)
        )
            return;

        if (type == "Weld")
        {
            var joint = a.AddComponent<FixedJoint2D>();
            joint.connectedBody = rbB;
        }
        else if (type == "Rope")
        {
            var joint = a.AddComponent<DistanceJoint2D>();
            joint.connectedBody = rbB;
            joint.distance = Vector2.Distance(a.transform.position, b.transform.position);
            joint.maxDistanceOnly = true;
        }
    }
}
