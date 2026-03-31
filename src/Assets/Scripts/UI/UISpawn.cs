using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISpawn : NetworkBehaviour
{
    [System.Serializable]
    public class OfficialPrefabEntry
    {
        public string name;
        public string category;
        public Sprite icon;
        public GameObject prefab;
    }

    [Header("Official Prefabs")]
    public List<OfficialPrefabEntry> officialPrefabs = new List<OfficialPrefabEntry>();

    [Header("UI Settings")]
    public GameObject scrollViewMenu;
    public Transform containerTransform;
    public GameObject buttonPrefab;
    public GameObject toolsMenu;

    private readonly List<GameObject> pool = new List<GameObject>();
    private string pendingModID = "";
    private GameObject pendingOfficialPrefab = null;
    private Camera mainCamera;
    private string currentCategory = "Live";

    private void Start()
    {
        mainCamera = Camera.main;
        if (ModManager.Instance != null)
        {
            if (ModManager.Instance.IsLoaded)
                GenerateMenu();
            else
                ModManager.Instance.OnModsLoadingCompleted += GenerateMenu;
        }
        else
        {
            GenerateMenu();
        }
    }

    private void Update()
    {
        HandlePendingPlacement();
    }

    private void HandlePendingPlacement()
    {
        if (string.IsNullOrEmpty(pendingModID) && pendingOfficialPrefab == null)
            return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            PlaceObject();

        if (Input.GetMouseButtonDown(1))
        {
            pendingModID = "";
            pendingOfficialPrefab = null;
        }
    }

    // --- CATEGORY MANAGEMENT ---
    public void SetCategory(string categoryName)
    {
        // Check if Left Shift is held to open tools menu instead
        if (Input.GetKey(KeyCode.LeftShift))
        {
            EnableTools();
            return;
        }

        currentCategory = categoryName;
        scrollViewMenu.SetActive(true);
        toolsMenu.SetActive(false);
        GenerateMenu();
    }

    public void EnableTools()
    {
        toolsMenu.SetActive(true);
        scrollViewMenu.SetActive(false);
    }

    // --- MENU GENERATION ---
    public void GenerateMenu()
    {
        if (containerTransform == null)
            return;

        foreach (var btn in pool)
            btn.SetActive(false);

        int poolIndex = 0;

        // Official Prefabs
        foreach (var entry in officialPrefabs)
        {
            if (currentCategory == "All" || entry.category == currentCategory)
                SetupOfficialButton(entry, poolIndex++);
        }

        // Mods
        if (ModManager.Instance != null)
        {
            foreach (var modEntry in ModManager.Instance.LoadedMods.Values)
            {
                string modCategory = modEntry.settings.category ?? "Other";
                if (currentCategory == "All" || modCategory == currentCategory)
                    SetupModButton(modEntry, poolIndex++);
            }
        }
    }

    private void SetupOfficialButton(OfficialPrefabEntry entry, int index)
    {
        GameObject btn = GetOrCreateButton(index);
        btn.name = "Btn_" + entry.name;

        Image img = GetIconImage(btn);
        Text label = btn.GetComponentInChildren<Text>();
        Button bComp = btn.GetComponent<Button>();

        if (label != null)
            label.text = entry.name;

        if (img != null && entry.icon != null)
        {
            img.gameObject.SetActive(true);
            img.sprite = entry.icon;
            img.preserveAspect = true;
            ScaleIcon(img, 50f);
        }

        bComp.onClick.RemoveAllListeners();
        bComp.onClick.AddListener(() =>
        {
            pendingModID = "";
            pendingOfficialPrefab = entry.prefab;
            Debug.Log($"Selected: {entry.name}");
        });
    }

    private void SetupModButton(ModData data, int index)
    {
        GameObject btn = GetOrCreateButton(index);
        btn.name = "Btn_" + data.settings.name;

        Image img = GetIconImage(btn);
        Text label = btn.GetComponentInChildren<Text>();
        Button bComp = btn.GetComponent<Button>();

        if (label != null)
            label.text = data.settings.name;

        if (img != null && data.sprite != null)
        {
            img.gameObject.SetActive(true);
            img.sprite = data.sprite;
            img.preserveAspect = true;
            ScaleIcon(img, 50f);
        }

        bComp.onClick.RemoveAllListeners();
        bComp.onClick.AddListener(() =>
        {
            pendingModID = data.folderID;
            pendingOfficialPrefab = null;
            Debug.Log($"Selected Mod: {data.settings.name}");
        });
    }

    // --- BUTTON HELPERS ---
    private GameObject GetOrCreateButton(int index)
    {
        if (index < pool.Count)
        {
            pool[index].SetActive(true);
            return pool[index];
        }

        GameObject btn = Instantiate(buttonPrefab, containerTransform);
        pool.Add(btn);
        return btn;
    }

    private Image GetIconImage(GameObject btn)
    {
        Image img = btn.transform.Find("Icon")?.GetComponent<Image>();
        return img ?? btn.GetComponentInChildren<Image>();
    }

    private void ScaleIcon(Image img, float maxSize)
    {
        if (img?.sprite == null)
            return;

        img.rectTransform.localScale = Vector3.one;
        float width = img.sprite.rect.width;
        float height = img.sprite.rect.height;
        float factor = maxSize / Mathf.Max(width, height);
        img.rectTransform.sizeDelta = new Vector2(width * factor, height * factor);
    }

    // --- SPAWNING ---
    private void PlaceObject()
    {
        if (mainCamera == null)
            return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 spawnPos = mainCamera.ScreenToWorldPoint(mousePos);
        spawnPos.z = 0f;

        // Official Prefab
        if (pendingOfficialPrefab != null)
        {
            int prefabIndex = officialPrefabs.FindIndex(e => e.prefab == pendingOfficialPrefab);
            if (prefabIndex >= 0)
                SpawnOfficialServerRpc(spawnPos, (byte)prefabIndex);

            pendingOfficialPrefab = null;
            return;
        }

        // Mod
        if (!string.IsNullOrEmpty(pendingModID))
        {
            if (
                NetworkManager.Singleton != null
                && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            )
                SpawnModServerRpc(pendingModID, spawnPos);
            else
                SpawnLocalMod(pendingModID, spawnPos);

            pendingModID = "";
        }
    }

    private void SpawnLocalMod(string modID, Vector3 pos)
    {
        if (ModManager.Instance == null)
            return;

        GameObject go = Instantiate(
            ModManager.Instance.networkedModPrefab,
            pos,
            Quaternion.identity
        );
        if (go.TryGetComponent(out NetworkedModObject nmo))
            nmo.SetModID(modID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnOfficialServerRpc(Vector3 position, byte prefabIndex)
    {
        if (prefabIndex >= officialPrefabs.Count)
            return;

        GameObject go = Instantiate(
            officialPrefabs[prefabIndex].prefab,
            position,
            Quaternion.identity
        );
        go.GetComponent<NetworkObject>()?.Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnModServerRpc(string modID, Vector3 position)
    {
        if (ModManager.Instance == null)
            return;

        GameObject go = Instantiate(
            ModManager.Instance.networkedModPrefab,
            position,
            Quaternion.identity
        );
        var netObj = go.GetComponent<NetworkObject>();
        netObj?.Spawn();

        if (go.TryGetComponent(out NetworkedModObject nmo))
            nmo.SetModID(modID);
    }
}
