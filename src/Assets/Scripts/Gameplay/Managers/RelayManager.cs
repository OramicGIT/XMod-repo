using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private bool isSigningIn = false;
    private TaskCompletionSource<bool> authReady = new TaskCompletionSource<bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Only destroy this component
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        await EnsureAuth();
    }

    private async Task InitializeAndSignIn()
    {
        if (
            UnityServices.State == ServicesInitializationState.Initialized
            && AuthenticationService.Instance.IsSignedIn
        )
        {
            authReady.TrySetResult(true);
            return;
        }

        if (isSigningIn)
            return;
        isSigningIn = true;

        // Reset TCS if faulted
        if (authReady.Task.IsFaulted || authReady.Task.IsCanceled)
            authReady = new TaskCompletionSource<bool>();

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            authReady.TrySetResult(true);
            Debug.Log("[RelayManager] Auth successful.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayManager] Auth failed: {e.Message}");
            authReady.TrySetException(e);
        }
        finally
        {
            isSigningIn = false;
        }
    }

    public async Task EnsureAuth()
    {
        if (
            authReady.Task.IsFaulted
            || authReady.Task.IsCanceled
            || UnityServices.State == ServicesInitializationState.Uninitialized
        )
        {
            await InitializeAndSignIn();
        }

        await authReady.Task;
    }

    public async Task<string> CreateRelay(int maxPlayers = 4)
    {
        try
        {
            await EnsureAuth();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();
            Debug.Log($"[RelayManager] Relay created. Join code: {joinCode}");
            return joinCode;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayManager] CreateRelay failed: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[RelayManager] JoinRelay: join code is empty.");
            return false;
        }

        try
        {
            await EnsureAuth();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(
                joinCode
            );

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
                Debug.LogWarning("[RelayManager] StartClient returned false.");
            return started;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayManager] JoinRelay failed: {e.Message}");
            return false;
        }
    }
}
