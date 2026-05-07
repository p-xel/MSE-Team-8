using UnityEngine;
using TMPro;
using Fusion;

// attaches to a textmeshpro object on the player prefab to indicate if it is their turn
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
            gameManager = FindObjectOfType<GameManager>();
            return;
        }

        if (networkObject != null && networkObject.IsValid)
        {
            string displayText = "";

            if (gameManager.isGameStarted)
            {
                if (gameManager.isPlayersTurn(networkObject.StateAuthority))
                {
                    // if this is our local player
                    if (networkObject.HasStateAuthority)
                    {
                        displayText = "your turn!";
                    }
                    else
                    {
                        // if looking at another player who is taking their turn
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
