using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MapLoader : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject waterPrefab;

    [Header("Cleaning Settings")]
    public List<GameObject> officialLevelObjects = new List<GameObject>();

    [Header("Loading Settings")]
    public int blocksPerFrame = 50;

    private readonly List<GameObject> spawnedBlocks = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        // Only server runs map loading
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        GameObject flag = GameObject.Find("CMap");

        // If CMap does NOT exist → destroy this loader completely
        if (flag == null)
        {
            Debug.Log("CMap not found → destroying MapLoader.");
            Destroy(gameObject); // or Destroy(this) if you only want to remove the script
            return;
        }

        // If CMap exists → proceed
        Debug.Log("CMap found → loading custom map.");

        Destroy(flag); // remove the marker

        ClearOfficialLevel();
        StartCoroutine(BuildFromData());
    }

    private void ClearOfficialLevel()
    {
        Debug.Log($"Starting cleanup. Items in list: {officialLevelObjects.Count}");

        for (int i = officialLevelObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = officialLevelObjects[i];

            if (obj == null)
                continue;

            if (obj.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
                else
                {
                    Destroy(obj);
                }
            }
            else
            {
                Destroy(obj);
            }
        }

        officialLevelObjects.Clear();
    }

    private IEnumerator BuildFromData()
    {
        if (string.IsNullOrEmpty(MapTransfer.JsonToLoad))
        {
            Debug.LogWarning("No map JSON data found.");
            yield break;
        }

        LevelData data;
        try
        {
            data = JsonUtility.FromJson<LevelData>(MapTransfer.JsonToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse map JSON: {e.Message}");
            yield break;
        }

        if (data?.walls == null || data.walls.Count == 0)
        {
            Debug.LogWarning("Map data contains no walls.");
            yield break;
        }

        // Try to get a more reliable spawn origin
        Vector3 spawnOrigin = Vector3.zero;

        if (Camera.main != null)
        {
            spawnOrigin = Camera.main.transform.position;
            spawnOrigin.z = 0f;
        }
        else
        {
            Debug.LogWarning("No main camera found → spawning at world origin (0,0,0)");
        }

        int spawnedCount = 0;

        for (int i = 0; i < data.walls.Count; i++)
        {
            WallData wall = data.walls[i];

            GameObject prefab = wall.type == BlockType.Water ? waterPrefab : wallPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"Missing prefab for wall type {wall.type} at index {i}");
                continue;
            }

            Vector3 spawnPosition = spawnOrigin + wall.p;

            Quaternion rotation = wall.r; // already a Quaternion?

            GameObject obj = Instantiate(prefab, spawnPosition, rotation);
            if (wall.s != Vector3.one && wall.s != Vector3.zero)
                obj.transform.localScale = wall.s;

            spawnedBlocks.Add(obj);

            if (obj.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (IsServer)
                    netObj.Spawn(true); // true = destroy with scene / with gameObject
            }

            spawnedCount++;

            if ((i + 1) % blocksPerFrame == 0)
            {
                Debug.Log($"Spawned {spawnedCount}/{data.walls.Count} blocks...");
                yield return null; // give frame time back
            }
        }

        Debug.Log($"Custom map loaded — {spawnedCount} blocks spawned.");
    }

    // Optional: cleanup when object is destroyed
    public override void OnDestroy()
    {
	base.OnDestroy();
        foreach (var block in spawnedBlocks)
        {
            if (block != null)
            {
                if (block.TryGetComponent<NetworkObject>(out var net))
                    if (net.IsSpawned)
                        net.Despawn(true);
                    else
                        Destroy(block);
                else
                    Destroy(block);
            }
        }
        spawnedBlocks.Clear();
    }
}
