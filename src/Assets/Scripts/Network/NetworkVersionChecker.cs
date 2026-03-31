using System.Text;
using Unity.Netcode;
using UnityEngine;

public class NetworkVersionChecker : MonoBehaviour
{
    [Header("Settings")]
    public string currentVersion = "1.0";

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
        else
        {
            Debug.LogWarning("[NetworkVersionChecker] No NetworkManager found in scene!");
        }
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response
    )
    {
        // Always approve server itself
        if (request.ClientNetworkId == NetworkManager.ServerClientId)
        {
            response.Approved = true;
            return;
        }

        // Check if client sent version data
        if (request.Payload == null || request.Payload.Length == 0)
        {
            response.Approved = false;
            response.Reason = "No version data provided.";
            Debug.LogWarning(
                $"[NetworkVersionChecker] Client {request.ClientNetworkId} rejected: {response.Reason}"
            );
            return;
        }

        // Decode version from payload
        string clientVersion = Encoding.ASCII.GetString(request.Payload);
        Debug.Log(
            $"[NetworkVersionChecker] Client {request.ClientNetworkId} connecting with version: {clientVersion}"
        );

        // Approve or reject based on version match
        if (clientVersion == currentVersion)
        {
            response.Approved = true;
            response.Reason = string.Empty;
        }
        else
        {
            response.Approved = false;
            response.Reason = $"Version mismatch! Host: {currentVersion}, Client: {clientVersion}";
            Debug.LogWarning(
                $"[NetworkVersionChecker] Client {request.ClientNetworkId} rejected: {response.Reason}"
            );
        }
    }
}
