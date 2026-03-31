using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public enum BlockType
{
    Wall,
    Water,
}

[Serializable]
public class WallData
{
    public Vector3 p;
    public Vector3 s;
    public Quaternion r;
    public BlockType type;
}

[Serializable]
public class LevelData
{
    public string creatorName = "User";
    public List<WallData> walls = new List<WallData>();
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Prefabs & Grid")]
    [SerializeField]
    private GameObject wallPrefab;

    [SerializeField]
    private GameObject waterPrefab;

    [SerializeField]
    private float gridSnap = 0.5f;

    [SerializeField]
    private float wallThickness = 0.4f;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private GameObject currentWall;
    private BlockType nextBlockType = BlockType.Wall;
    private Camera mainCam;

    // Auto-save
    private float autoSaveTimer = 0f;
    private const float autoSaveInterval = 600f;

    private bool IsNetServer =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    private bool IsNetActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        mainCam = Camera.main;
    }

    private void Update()
    {
        HandleInput();
        HandleEditing();
        HandleAutoSave();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ExitToMenu();

        // File ops only for offline or server
        if (IsNetActive && !IsNetServer)
            return;

        if (Input.GetKeyDown(KeyCode.L))
            LoadLevel("world_current");
        if (Input.GetKeyDown(KeyCode.S))
            SaveLevel("world_current");
        if (Input.GetKeyDown(KeyCode.Z))
            Undo();
    }

    private void HandleAutoSave()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            SaveLevel("world_current_autosave");
        }
    }

    private void ExitToMenu()
    {
        NetworkManager.Singleton?.Shutdown();
        SceneManager.LoadScene("Menu");
    }

    public void SaveLevel(string fileName)
    {
        LevelData data = new LevelData { creatorName = "User", walls = GetAllWallData() };
        File.WriteAllText(GetSavePath(fileName), JsonUtility.ToJson(data, true));
        Debug.Log($"<color=green>Saved: {fileName}</color>");
    }

    public void LoadLevel(string fileName)
    {
        string path = GetSavePath(fileName);
        if (!File.Exists(path))
            return;

        LevelData data = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
        ClearLevel();

        foreach (var wall in data.walls)
            SpawnBlock(wall.p, wall.type, wall);
    }

    private string GetSavePath(string name) =>
        Path.Combine(Application.persistentDataPath, name + ".json");

    // --- Editing & Spawning ---

    private void HandleEditing()
    {
        if (Input.GetKeyDown(KeyCode.W))
            nextBlockType = BlockType.Water;
        if (Input.GetKeyDown(KeyCode.Q))
            nextBlockType = BlockType.Wall;

        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null)
            return;

        Vector3 mousePos = SnapToGrid(mainCam.ScreenToWorldPoint(Input.mousePosition));
        mousePos.z = 0;

        if (Input.GetMouseButtonDown(0))
            SpawnBlock(mousePos, nextBlockType);

        if (Input.GetMouseButton(0) && currentWall != null)
            StretchCurrentWall(mousePos);

        if (Input.GetMouseButtonUp(0))
        {
            if (currentWall != null && currentWall.transform.localScale.x <= 0.11f)
                Undo();
            currentWall = null;
        }
    }

    private void StretchCurrentWall(Vector3 mousePos)
    {
        Vector2 dir = (Vector2)mousePos - (Vector2)currentWall.transform.position;
        float dist = Mathf.Max(0.1f, dir.magnitude);

        currentWall.transform.localScale = new Vector3(dist, wallThickness, 1f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        currentWall.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SpawnBlock(Vector3 pos, BlockType type, WallData data = null)
    {
        GameObject prefab = type == BlockType.Water ? waterPrefab : wallPrefab;
        GameObject obj = Instantiate(
            prefab,
            data != null ? data.p : pos,
            data != null ? data.r : Quaternion.identity
        );
        obj.transform.localScale = data != null ? data.s : Vector3.one;

        if (!obj.TryGetComponent<LevelBlockInfo>(out var info))
            info = obj.AddComponent<LevelBlockInfo>();
        info.type = type;

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (IsNetActive && IsNetServer && netObj != null)
            netObj.Spawn(true);

        spawnedObjects.Add(obj);
        currentWall = obj;
    }

    public List<WallData> GetAllWallData()
    {
        List<WallData> data = new List<WallData>();
        foreach (var obj in spawnedObjects)
        {
            if (obj == null)
                continue;
            var info = obj.GetComponent<LevelBlockInfo>();
            data.Add(
                new WallData
                {
                    p = obj.transform.position,
                    s = obj.transform.localScale,
                    r = obj.transform.rotation,
                    type = info != null ? info.type : BlockType.Wall,
                }
            );
        }
        return data;
    }

    public void ClearLevel()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj == null)
                continue;
            if (IsNetActive && IsNetServer && obj.TryGetComponent(out NetworkObject net))
                net.Despawn();
            else
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    private void Undo()
    {
        if (spawnedObjects.Count == 0)
            return;

        GameObject last = spawnedObjects[spawnedObjects.Count - 1];
        if (last != null)
        {
            if (IsNetActive && IsNetServer && last.TryGetComponent(out NetworkObject net))
                net.Despawn();
            else
                Destroy(last);
        }
        spawnedObjects.RemoveAt(spawnedObjects.Count - 1);
    }

    private Vector3 SnapToGrid(Vector3 pos) =>
        new Vector3(
            Mathf.Round(pos.x / gridSnap) * gridSnap,
            Mathf.Round(pos.y / gridSnap) * gridSnap,
            0
        );
}
