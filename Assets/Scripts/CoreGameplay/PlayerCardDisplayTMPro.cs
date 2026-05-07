using UnityEngine;
using UnityEngine.UI;
using TMPro;

// attaches to a textmeshpro object to display a specific card from the player's hand
public class PlayerCardDisplayTMPro : MonoBehaviour
{
    public PlayerHand playerHand;
    public int cardIndex;
    
    private TextMeshPro textMesh;
    private TextMeshProUGUI textMeshGui;
    private Button button;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textMeshGui = GetComponent<TextMeshProUGUI>();
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(onClick);
        }
    }

    void onClick()
    {
        if (PlayerHand.localHand != null)
        {
            PlayerHand.localHand.selectMyCard(cardIndex);
        }
    }

    void Update()
    {
        // wait until playerHand is assigned and valid
        if (playerHand != null && playerHand.Object != null && playerHand.Object.IsValid)
        {
            if (cardIndex >= 0 && cardIndex < playerHand.myCards.Length)
            {
                // only show the actual card value if this is our local player
                if (playerHand.Object.HasStateAuthority)
                {
                    CardData card = playerHand.myCards[cardIndex];
                    string displayText = card.number > 0 ? card.ToString() : "empty";

                    if (textMesh != null) textMesh.text = displayText;
                    if (textMeshGui != null) textMeshGui.text = displayText;
                }
                else
                {
                    // hide cards that belong to other players
                    string hiddenText = "hidden";
                    
                    if (textMesh != null) textMesh.text = hiddenText;
                    if (textMeshGui != null) textMeshGui.text = hiddenText;
                }
            }
        }
    }
}
