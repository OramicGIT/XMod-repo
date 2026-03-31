using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// =============================================
//  MOD SETTINGS
// =============================================
[Serializable]
public class ModSettings
{
    public string name = "Unnamed Mod";
    public string category = "Other";
    public string textureName;
    public bool isAlive = false;
    public bool isSolid = true;
    public bool isPhysics = false;
    public float mass = 1f;
    public float friction = 0.4f;
    public float bounciness = 0f;
    public bool isExplosive = false;
    public float explosionRadius = 3f;
    public float explosionForce = 500f;
    public bool explosionDestroySelf = true;
    public string[] animationFrames;
    public float animationSpeed = 8f;
}

// =============================================
//  MOD DATA
// =============================================
public class ModData
{
    public ModSettings settings;
    public Sprite sprite;
    public Sprite[] animationFrames;
    public string folderID;
    public string folderPath;
}

// =============================================
//  MOD MANAGER
// =============================================
public class ModManager : MonoBehaviour
{
    public static ModManager Instance { get; private set; }

    [Header("Refs")]
    public GameObject networkedModPrefab;
    public Sprite missingTextureSprite;

    [Header("Limits")]
    public int maxTextureSize = 512;
    public long maxModSizeBytes = 10 * 1024 * 1024;

    public bool IsLoaded { get; private set; }
    public event Action OnModsLoadingStarted;
    public event Action OnModsLoadingCompleted;

    private readonly Dictionary<string, ModData> _mods = new();
    private string ModsRootPath => Path.Combine(Application.persistentDataPath, "Mods");
    public IReadOnlyDictionary<string, ModData> LoadedMods => _mods;

    // =============================================
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        StartCoroutine(LoadAllModsRoutine());
    }

    // =============================================
    //  Loading
    // =============================================
    private IEnumerator LoadAllModsRoutine()
    {
        OnModsLoadingStarted?.Invoke();

        if (!Directory.Exists(ModsRootPath))
        {
            Directory.CreateDirectory(ModsRootPath);
            FinishLoading();
            yield break;
        }

        foreach (string folder in Directory.GetDirectories(ModsRootPath))
            yield return StartCoroutine(LoadSingleModRoutine(folder));

        FinishLoading();
    }

    private IEnumerator LoadSingleModRoutine(string folderPath)
    {
        string jsonPath = Path.Combine(folderPath, "info.json");
        if (!File.Exists(jsonPath))
            yield break;

        if (GetFolderSize(folderPath) > maxModSizeBytes)
        {
            Debug.LogWarning($"[ModManager] Mod '{folderPath}' exceeds size limit, skipping.");
            yield break;
        }

        ModSettings settings;
        try
        {
            settings = JsonUtility.FromJson<ModSettings>(File.ReadAllText(jsonPath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ModManager] Error parsing '{folderPath}': {e.Message}");
            yield break;
        }

        string folderID = Path.GetFileName(folderPath);
        Sprite mainSprite = LoadSprite(folderPath, settings.textureName) ?? missingTextureSprite;

        Sprite[] animFrames = null;
        if (settings.animationFrames is { Length: > 0 })
        {
            animFrames = new Sprite[settings.animationFrames.Length];
            for (int i = 0; i < settings.animationFrames.Length; i++)
            {
                animFrames[i] =
                    LoadSprite(folderPath, settings.animationFrames[i]) ?? missingTextureSprite;
                yield return null;
            }
        }

        _mods[folderID] = new ModData
        {
            settings = settings,
            sprite = mainSprite,
            animationFrames = animFrames,
            folderID = folderID,
            folderPath = folderPath,
        };

        yield return null;
    }

    private void FinishLoading()
    {
        IsLoaded = true;
        OnModsLoadingCompleted?.Invoke();
        Debug.Log($"[ModManager] Loading complete. Mods loaded: {_mods.Count}");
    }

    // =============================================
    //  Public API
    // =============================================
    public ModData GetMod(string id)
    {
        if (_mods.TryGetValue(id, out ModData data))
            return data;
        return new ModData
        {
            settings = new ModSettings { name = "Missing Mod", isSolid = true },
            sprite = missingTextureSprite,
            folderID = "missing",
        };
    }

    public void ReloadMod(string folderID)
    {
        if (!_mods.TryGetValue(folderID, out ModData existing))
            return;
        string path = existing.folderPath;
        _mods.Remove(folderID);
        StartCoroutine(LoadSingleModRoutine(path));
    }

    public IEnumerable<ModData> GetModsByCategory(string category)
    {
        foreach (var mod in _mods.Values)
            if (mod.settings.category == category)
                yield return mod;
    }

    // =============================================
    //  Explosions
    // =============================================
    public void HandleExplosion(Vector2 position, float radius, float force, GameObject source)
    {
        foreach (var hit in Physics2D.OverlapCircleAll(position, radius))
        {
            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.WakeUp();
                Vector2 dir = rb.worldCenterOfMass - position;
                float dist = dir.magnitude;
                if (dist < radius)
                {
                    float falloff = 1f - Mathf.Clamp01(dist / radius);
                    rb.AddForce(dir.normalized * force * falloff * falloff, ForceMode2D.Impulse);
                }
            }
        }
    }

    public void HandleExplosion(ModData mod, Vector2 position, GameObject source)
    {
        if (!mod.settings.isExplosive)
            return;
        HandleExplosion(
            position,
            mod.settings.explosionRadius,
            mod.settings.explosionForce,
            source
        );
    }

    // =============================================
    //  Helpers
    // =============================================
    private Sprite LoadSprite(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;
        string fullPath = Path.Combine(folderPath, fileName);
        if (!File.Exists(fullPath))
            return null;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(fullPath)))
            return null;

        tex.filterMode = FilterMode.Point;
        tex.Apply(true, true);
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private long GetFolderSize(string folderPath)
    {
        long size = 0;
        foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            size += new FileInfo(file).Length;
        return size;
    }
}
