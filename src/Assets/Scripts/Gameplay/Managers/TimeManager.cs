using Unity.Netcode;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; } // get the singleton

    private float currentTimeScale = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Apply the current time scale safely
        ApplyTimeScale(currentTimeScale);

        // Check if the NetworkManager singleton exists
        if (NetworkManager.Singleton != null)
        {
            var cm = NetworkManager.Singleton.CustomMessagingManager;

            if (cm != null)
            {
                cm.RegisterNamedMessageHandler("TimeChange", OnTimeChangeMessage);

                if (NetworkManager.Singleton.IsServer)
                {
                    cm.RegisterNamedMessageHandler("TimeChangeRequest", OnTimeChangeRequest);
                }
            }

            // Register client connect callback safely
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            Debug.LogWarning("[TimeManager] NetworkManager.Singleton is null at Start.");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            var cm = NetworkManager.Singleton.CustomMessagingManager;
            cm.UnregisterNamedMessageHandler("TimeChange");
            cm.UnregisterNamedMessageHandler("TimeChangeRequest");
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void ApplyTimeScale(float newScale)
    {
        currentTimeScale = newScale;
        Time.timeScale = newScale;
        Time.fixedDeltaTime = 0.02f * newScale;
        Debug.Log($"Applied TimeScale: {newScale}");
    }

    public void RestoreTime() => RequestTimeChange(1f);

    public void SlowMo() => RequestTimeChange(0.5f);

    public void Pause() => RequestTimeChange(0f);

    private void RequestTimeChange(float targetScale)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            BroadcastTimeChange(targetScale);
        }
        else
        {
            var writer = new FastBufferWriter(sizeof(float), Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(targetScale);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                "TimeChangeRequest",
                NetworkManager.ServerClientId,
                writer
            );
        }
    }

    private void BroadcastTimeChange(float newScale)
    {
        ApplyTimeScale(newScale);

        var writer = new FastBufferWriter(sizeof(float), Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(newScale);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("TimeChange", writer);
    }

    private void OnTimeChangeMessage(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out float newScale);
        ApplyTimeScale(newScale);
    }

    private void OnTimeChangeRequest(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out float requestedScale);
        BroadcastTimeChange(requestedScale);
    }

    private void OnClientConnected(ulong clientId)
    {
        var writer = new FastBufferWriter(sizeof(float), Unity.Collections.Allocator.Temp);
        writer.WriteValueSafe(currentTimeScale);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            "TimeChange",
            clientId,
            writer
        );
    }
}
