using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GameDeck : NetworkBehaviour
{
    [Networked, Capacity(52)]
    public NetworkLinkedList<CardData> deckCards { get; }

    public Text deckText;

    public void initDeck()
    {
        deckCards.Clear();

        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                if (rank >= 2 && rank <= 5) continue;

                CardData card = new CardData();
                card.color = (CardColor)suit;
                card.number = rank;

                if (rank == 1) card.gameValue = 11;
                else if (rank >= 10) card.gameValue = 10;
                else card.gameValue = rank;

                deckCards.Add(card);
            }
        }

        shuffleDeck();
    }

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
            deckText.text = $"cards remaining: {deckCards.Count}";
    }
}
