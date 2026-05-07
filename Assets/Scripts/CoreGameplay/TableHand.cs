using Fusion;
using UnityEngine;
using UnityEngine.UI;

// manages the 3 shared cards on the table
public class TableHand : NetworkBehaviour
{
    // networked array of 3 cards
    [Networked, Capacity(3)]
    public NetworkArray<CardData> tableCards { get; }

    // ui text elements to display the cards on the table
    public Text[] cardTexts = new Text[3];

    // attempts to take authority over the table
    public void requestAuthority()
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }

    // draws 3 initial cards from the gamedeck. caller needs state authority over both table and deck.
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

    // called by a player's client to ask the master client (table owner) to swap a card
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_SwapCard(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        if (Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand))
        {
            if (tableIndex >= 0 && tableIndex < 3)
            {
                CardData oldTableCard = tableCards[tableIndex];
                tableCards.Set(tableIndex, playerCard);
                
                // send the old table card back to the player
                playerHand.Rpc_ReceiveSwappedCard(oldTableCard, handIndex);
            }
        }
    }

    public override void Render()
    {
        // update ui texts to reflect the networked state
        for (int i = 0; i < 3; i++)
        {
            if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null)
            {
                // check if the card is valid (number > 0)
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
