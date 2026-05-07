using UnityEngine;
using UnityEngine.UI;
using TMPro;

// attaches to a textmeshpro object to display a specific table card
public class CardDisplayTMPro : MonoBehaviour
{
    public TableHand tableHand;
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
            PlayerHand.localHand.selectTableCard(cardIndex);
        }
    }

    void Update()
    {
        if (tableHand != null && tableHand.Object != null && tableHand.Object.IsValid)
        {
            if (cardIndex >= 0 && cardIndex < tableHand.tableCards.Length)
            {
                CardData card = tableHand.tableCards[cardIndex];
                string displayText = card.number > 0 ? card.ToString() : "empty";

                if (textMesh != null) textMesh.text = displayText;
                if (textMeshGui != null) textMeshGui.text = displayText;
            }
        }
    }
}
