using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RelayUI : MonoBehaviour
{
    [Header("Settings")]
    public NetworkVersionChecker versionChecker;
    public string uiHideTag = "RelayUI_Canvas";

    private GameObject mainCanvasGroup;
    private InputField joinCodeInputField;
    private Text displayCodeText;
    private Button joinButton;
    private Button singlePlayerButton;
    private bool isUiVisible = true;

    private async void Start()
    {
        AssignReferencesByTags();

        // Wait for NetworkManager and RelayManager
        float timeout = 5f;
        while ((RelayManager.Instance == null || NetworkManager.Singleton == null) && timeout > 0f)
        {
            await Task.Delay(100);
            timeout -= 0.1f;
        }

        if (RelayManager.Instance == null || NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayUI] Missing Network Components.");
            return;
        }

        try
        {
            await RelayManager.Instance.EnsureAuth();
            await StartRelayHost();
            if (joinButton != null)
                joinButton.interactable = true; // Enable after loading
            if (singlePlayerButton != null)
                singlePlayerButton.interactable = true; // Enable after loading
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayUI] Initialization failed: {e.Message}");
        }
    }

    private void AssignReferencesByTags()
    {
        mainCanvasGroup = GameObject.FindWithTag("RelayUI_Canvas");
        joinCodeInputField = GameObject.FindWithTag("RelayUI_Input")?.GetComponent<InputField>();
        displayCodeText = GameObject.FindWithTag("RelayUI_Display")?.GetComponent<Text>();
        joinButton = GameObject.FindWithTag("RelayUI_JoinBtn")?.GetComponent<Button>();
        singlePlayerButton = GameObject.FindWithTag("RelayUI_SPBtn")?.GetComponent<Button>();

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => StartRelayJoin());
            joinButton.interactable = false; // Disable until loaded
        }

        if (singlePlayerButton != null)
        {
            singlePlayerButton.onClick.RemoveAllListeners();
            singlePlayerButton.onClick.AddListener(HideUIByTag);
            singlePlayerButton.interactable = false; // Disable until loaded
        }

        if (!mainCanvasGroup || !joinCodeInputField || !displayCodeText || !joinButton)
        {
            Debug.LogWarning("[RelayUI] Some UI references are missing. Check tags!");
        }
    }

    public async Task StartRelayHost()
    {
        if (displayCodeText == null)
            return;

        displayCodeText.text = "...";

        string code = await RelayManager.Instance.CreateRelay();

        if (!string.IsNullOrEmpty(code))
        {
            displayCodeText.text = code;
            GUIUtility.systemCopyBuffer = code; // Copy to clipboard
        }
        else
        {
            displayCodeText.text = "Error: Relay creation failed";
        }
    }

    private void ToggleUI()
    {
        if (mainCanvasGroup == null)
            return;

        isUiVisible = !isUiVisible;
        mainCanvasGroup.SetActive(isUiVisible);
    }

    public void HideUIByTag()
    {
        var uiObject = GameObject.FindWithTag(uiHideTag);
        if (uiObject == null)
        {
            Debug.LogWarning($"[RelayUI] Could not find UI object with tag '{uiHideTag}' to hide.");
            return;
        }
        uiObject.SetActive(false);
        isUiVisible = false;
    }

    public async void StartRelayJoin()
    {
        if (joinCodeInputField == null || RelayManager.Instance == null)
            return;

        string code = joinCodeInputField.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            if (displayCodeText != null)
                displayCodeText.text = "Error: Empty join code";
            return;
        }

        if (displayCodeText != null)
            displayCodeText.text = "...";

        // Send version info to host
        if (versionChecker != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(
                versionChecker.currentVersion
            );
        }

        bool success = await RelayManager.Instance.JoinRelay(code);

        if (!success && displayCodeText != null)
            displayCodeText.text = "Error: Failed to join relay";
    }
}
