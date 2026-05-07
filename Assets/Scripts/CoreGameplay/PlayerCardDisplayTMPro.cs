using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        Debug.Log($"clicked card {cardIndex}", this);
        if (PlayerHand.localHand != null)
        {
            PlayerHand.localHand.selectMyCard(cardIndex);
        }
    }

    void Update()
    {
        if (playerHand != null && playerHand.Object != null && playerHand.Object.IsValid)
        {
            if (cardIndex >= 0 && cardIndex < playerHand.myCards.Length)
            {
                // local player sees their real cards; remote players' hands stay hidden
                if (playerHand.Object.HasStateAuthority)
                {
                    CardData card = playerHand.myCards[cardIndex];
                    string displayText = card.number > 0 ? card.ToString() : "empty";

                    if (textMesh != null) textMesh.text = displayText;
                    if (textMeshGui != null) textMeshGui.text = displayText;
                }
                else
                {
                    string hiddenText = "hidden";

                    if (textMesh != null) textMesh.text = hiddenText;
                    if (textMeshGui != null) textMeshGui.text = hiddenText;
                }
            }
        }
    }
}
