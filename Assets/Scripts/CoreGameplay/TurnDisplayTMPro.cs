using UnityEngine;
using TMPro;
using Fusion;

public class TurnDisplayTMPro : MonoBehaviour
{
    private TextMeshPro textMesh;
    private TextMeshProUGUI textMeshGui;
    private NetworkObject networkObject;
    private GameManager gameManager;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textMeshGui = GetComponent<TextMeshProUGUI>();
        networkObject = GetComponentInParent<NetworkObject>();
    }

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
            return;
        }

        if (networkObject != null && networkObject.IsValid)
        {
            string displayText = "";

            if (gameManager.isGameStarted)
            {
                if (gameManager.isPlayersTurn(networkObject.StateAuthority))
                {
                    // own player → first-person prompt; remote player → spectator prompt
                    if (networkObject.HasStateAuthority)
                    {
                        displayText = "your turn!";
                    }
                    else
                    {
                        displayText = "playing...";
                    }
                }
                else
                {
                    displayText = "waiting...";
                }
            }

            if (textMesh != null) textMesh.text = displayText;
            if (textMeshGui != null) textMeshGui.text = displayText;
        }
    }
}
