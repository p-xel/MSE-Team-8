using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class TableHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> tableCards { get; }

    public Text[] cardTexts = new Text[3];

    public void requestAuthority()
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }

    // caller needs state authority over both this table AND the deck
    public void initialize(GameDeck deck)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("cannot initialize table without state authority!");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            tableCards.Set(i, deck.drawCard());
        }
    }

    // any client requests the swap; the table owner performs it and returns the displaced card
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_SwapCard(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        if (Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand))
        {
            if (tableIndex >= 0 && tableIndex < 3)
            {
                CardData oldTableCard = tableCards[tableIndex];
                tableCards.Set(tableIndex, playerCard);

                playerHand.Rpc_ReceiveSwappedCard(oldTableCard, handIndex);
            }
        }
    }

    public override void Render()
    {
        for (int i = 0; i < 3; i++)
        {
            if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null)
            {
                if (tableCards[i].number > 0)
                {
                    cardTexts[i].text = tableCards[i].ToString();
                }
                else
                {
                    cardTexts[i].text = "empty";
                }
            }
        }
    }
}
