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

        if (playerHand == null)
            playerHand = GetComponentInParent<PlayerHand>();

        if (button != null)
            button.onClick.AddListener(onClick);
    }

    void onClick()
    {
        if (PlayerHand.localHand != null)
            PlayerHand.localHand.selectMyCard(cardIndex);
    }

    void Update()
    {
        if (playerHand == null || playerHand.Object == null || !playerHand.Object.IsValid) return;
        if (cardIndex < 0 || cardIndex >= playerHand.myCards.Length) return;

        if (playerHand.Object.HasStateAuthority)
        {
            CardData card = playerHand.myCards[cardIndex];
            string displayText = card.number > 0 ? card.ToString() : "empty";
            if (textMesh != null) textMesh.text = displayText;
            if (textMeshGui != null) textMeshGui.text = displayText;
        }
        else
        {
            if (textMesh != null) textMesh.text = "hidden";
            if (textMeshGui != null) textMeshGui.text = "hidden";
        }
    }
}
