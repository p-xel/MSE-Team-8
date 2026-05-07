using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GameDeck : NetworkBehaviour
{
    [Networked, Capacity(52)]
    public NetworkLinkedList<CardData> deckCards { get; }

    public Text deckText;

    public override void Spawned()
    {
        // only the state authority owns the deck and seeds it
        if (Object.HasStateAuthority)
        {
            initDeck();
        }
    }

    public void initDeck()
    {
        deckCards.Clear();

        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                CardData card = new CardData();
                card.color = (CardColor)suit;
                card.number = rank;

                // game-31 scoring: A=11, J/Q/K=10, others=face value
                if (rank == 1) card.gameValue = 11;
                else if (rank >= 10) card.gameValue = 10;
                else card.gameValue = rank;

                deckCards.Add(card);
            }
        }

        shuffleDeck();
    }

    // Fisher-Yates shuffle
    private void shuffleDeck()
    {
        for (int i = 0; i < deckCards.Count; i++)
        {
            CardData temp = deckCards[i];
            int randomIndex = Random.Range(i, deckCards.Count);
            deckCards.Set(i, deckCards[randomIndex]);
            deckCards.Set(randomIndex, temp);
        }
    }

    public void requestAuthority()
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }

    // requires state authority; returns an empty CardData (number==0) if called without it or on an empty deck
    public CardData drawCard()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("cannot draw without state authority!");
            return new CardData();
        }

        if (deckCards.Count > 0)
        {
            CardData drawn = deckCards[0];
            deckCards.Remove(drawn);
            return drawn;
        }

        Debug.LogWarning("deck is empty!");
        return new CardData();
    }

    // any client can ask, only the deck owner draws and sends back via Rpc_ReceiveInitialHand
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_Draw3CardsForPlayer(NetworkBehaviourId playerHandId)
    {
        if (Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand))
        {
            CardData c1 = drawCard();
            CardData c2 = drawCard();
            CardData c3 = drawCard();

            playerHand.Rpc_ReceiveInitialHand(c1, c2, c3);
        }
    }

    public override void Render()
    {
        if (deckText != null)
        {
            deckText.text = $"cards remaining: {deckCards.Count}";
        }
    }
}
