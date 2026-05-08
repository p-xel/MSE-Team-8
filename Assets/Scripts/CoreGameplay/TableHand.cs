using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class TableHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> tableCards { get; }

    public Text[] cardTexts = new Text[3];

    public void initialize(GameDeck deck)
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < 3; i++)
            tableCards.Set(i, deck.drawCard());
    }

    public void performSwap(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        if (tableIndex < 0 || tableIndex >= 3) return;
        if (!Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand)) return;

        CardData oldTableCard = tableCards[tableIndex];
        tableCards.Set(tableIndex, playerCard);

        if (playerHand.Object.HasStateAuthority)
            playerHand.myCards.Set(handIndex, oldTableCard);
        else
            playerHand.Rpc_ReceiveSwappedCard(oldTableCard, handIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_SwapCard(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        performSwap(tableIndex, playerCard, playerHandId, handIndex);
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
