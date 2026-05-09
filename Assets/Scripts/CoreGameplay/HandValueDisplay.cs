using UnityEngine;
using TMPro;

public class HandValueDisplay : MonoBehaviour
{
    private TextMeshPro textMesh;
    private TextMeshProUGUI textMeshGui;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textMeshGui = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        PlayerHand hand = PlayerHand.localHand;
        if (hand == null || hand.Object == null || !hand.Object.IsValid) return;

        string display = computeDisplay(hand);
        if (textMesh != null) textMesh.text = display;
        if (textMeshGui != null) textMeshGui.text = display;
    }

    private string computeDisplay(PlayerHand hand)
    {
        CardData c0 = hand.myCards[0];
        CardData c1 = hand.myCards[1];
        CardData c2 = hand.myCards[2];

        if (c0.number == 0 && c1.number == 0 && c2.number == 0)
            return "hand: 0";

        if (c0.number > 0 && c0.number == c1.number && c1.number == c2.number)
            return "hand: 30½ (invicibility)";

        int[] suitTotals = new int[4];
        for (int i = 0; i < 3; i++)
        {
            CardData card = hand.myCards[i];
            if (card.number > 0)
                suitTotals[(int)card.color] += card.gameValue;
        }

        // hand value is best possible combination of suits
        int best = 0;
        for (int i = 0; i < 4; i++)
            if (suitTotals[i] > best) best = suitTotals[i];

        return $"hand: {best}";
    }
}
