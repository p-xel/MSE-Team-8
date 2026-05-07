using Fusion;
using UnityEngine;
using UnityEngine.UI;

// handles the main deck of cards for the game
public class GameDeck : NetworkBehaviour
{
    // the networked list of cards
    [Networked, Capacity(52)]
    public NetworkLinkedList<CardData> deckCards { get; }

    // optional ui text to display remaining cards
    public Text deckText;

    public override void Spawned()
    {
        // only the state authority (master client) initializes the deck
        if (Object.HasStateAuthority)
        {
            initDeck();
        }
    }

    // populates the deck with a standard 52-card set
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
                
                // assign default game value (e.g., face cards are 10, ace is 11 for game 31)
                if (rank == 1) card.gameValue = 11;
                else if (rank >= 10) card.gameValue = 10;
                else card.gameValue = rank;

                deckCards.Add(card);
            }
        }

        shuffleDeck();
    }

    // simple fisher-yates shuffle
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

    // attempts to take authority over the deck
    public void requestAuthority()
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }
    }

    // draws a card from the top of the deck. requires state authority.
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

    // called by a player's client to ask the master client (deck owner) for 3 cards
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_Draw3CardsForPlayer(NetworkBehaviourId playerHandId)
    {
        if (Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand))
        {
            CardData c1 = drawCard();
            CardData c2 = drawCard();
            CardData c3 = drawCard();
            
            // send the cards back to the player who owns the hand
            playerHand.Rpc_ReceiveInitialHand(c1, c2, c3);
        }
    }

    public override void Render()
    {
        // update ui text if assigned
        if (deckText != null)
        {
            deckText.text = $"cards remaining: {deckCards.Count}";
        }
    }
}
