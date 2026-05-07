using Fusion;
using UnityEngine;
using UnityEngine.UI;

// manages the 3 cards held by a single player
public class PlayerHand : NetworkBehaviour
{
    // networked array of 3 cards for the player
    [Networked, Capacity(3)]
    public NetworkArray<CardData> myCards { get; }

    // ui text elements to display the cards locally
    public Text[] cardTexts = new Text[3];
    public Canvas playerHandCanvas;

    private GameManager gameManager;
    private GameDeck gameDeck;
    private bool hasRequestedInitialCards = false;
    
    public static PlayerHand localHand { get; private set; }
    private int selectedMyCardIndex = -1;

    public override void Spawned()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        gameDeck = FindAnyObjectByType<GameDeck>();
        
        if (Object.HasStateAuthority)
        {
            localHand = this;
        }
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // isolate overlay ui for multi-peer mode
        if (playerHandCanvas != null)
        {
            playerHandCanvas.enabled = Runner.GetVisible();
        }

        // only process keyboard input if this is the player we are currently viewing in multi-peer mode
        if (Object.HasStateAuthority && gameManager != null && Runner.GetVisible())
        {
            if (gameManager.isPlayersTurn(Object.StateAuthority))
            {
                // skip turn if 's' is pressed
                if (Input.GetKeyDown(KeyCode.S))
                {
                    Debug.Log("skipping turn.");
                    selectedMyCardIndex = -1;
                    gameManager.Rpc_EndTurn();
                }
            }
        }
    }

    // checks if the hand is empty (first round)
    public bool isHandEmpty()
    {
        return myCards[0].number == 0 && myCards[1].number == 0 && myCards[2].number == 0;
    }

    // player action: draw 3 cards on their first turn
    public void draw3Cards(GameDeck deck, GameManager manager)
    {
        if (Object.HasStateAuthority)
        {
            if (manager.isPlayersTurn(Object.StateAuthority))
            {
                if (isHandEmpty())
                {
                    deck.Rpc_Draw3CardsForPlayer(Id);
                    manager.Rpc_EndTurn();
                }
                else
                {
                    Debug.LogWarning("cannot draw 3 cards. hand is not empty.");
                }
            }
            else
            {
                Debug.LogWarning("it is not your turn!");
            }
        }
    }

    // player action: swap 1 card on subsequent turns
    public void swapCard(TableHand table, int myCardIndex, int tableCardIndex, GameManager manager)
    {
        if (Object.HasStateAuthority)
        {
            if (manager.isPlayersTurn(Object.StateAuthority))
            {
                if (!isHandEmpty())
                {
                    CardData cardToSwap = myCards[myCardIndex];
                    table.Rpc_SwapCard(tableCardIndex, cardToSwap, Id, myCardIndex);
                    manager.Rpc_EndTurn();
                }
                else
                {
                    Debug.LogWarning("cannot swap cards. hand is empty. you must draw first.");
                }
            }
            else
            {
                Debug.LogWarning("it is not your turn!");
            }
        }
    }

    // called by ui button to select a card in hand
    public void selectMyCard(int index)
    {
        if (Object.HasStateAuthority && gameManager != null && gameManager.isPlayersTurn(Object.StateAuthority))
        {
            if (!isHandEmpty())
            {
                selectedMyCardIndex = index;
                Debug.Log($"selected hand card {index}");
            }
        }
    }

    // called by ui button to select a table card and perform swap
    public void selectTableCard(int tableIndex)
    {
        if (Object.HasStateAuthority && gameManager != null && gameManager.isPlayersTurn(Object.StateAuthority))
        {
            if (selectedMyCardIndex != -1)
            {
                TableHand table = FindAnyObjectByType<TableHand>();
                if (table != null)
                {
                    swapCard(table, selectedMyCardIndex, tableIndex, gameManager);
                    selectedMyCardIndex = -1; // reset after swap
                }
            }
            else
            {
                Debug.LogWarning("select a card from your hand first!");
            }
        }
    }

    // called by the master client to give this player 3 cards
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ReceiveInitialHand(CardData c1, CardData c2, CardData c3)
    {
        myCards.Set(0, c1);
        myCards.Set(1, c2);
        myCards.Set(2, c3);
    }

    // called by the master client to give this player the swapped card
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ReceiveSwappedCard(CardData newCard, int myCardIndex)
    {
        myCards.Set(myCardIndex, newCard);
    }

    public override void Render()
    {
        // auto-draw logic for the first turn
        if (Object.HasStateAuthority && gameManager != null && gameDeck != null)
        {
            if (gameManager.isPlayersTurn(Object.StateAuthority) && isHandEmpty() && !hasRequestedInitialCards)
            {
                hasRequestedInitialCards = true;
                draw3Cards(gameDeck, gameManager);
            }
        }

        // update ui texts to reflect the networked state
        // we check hasstateauthority so we only update the ui for our local player
        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < 3; i++)
            {
                if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null)
                {
                    if (myCards[i].number > 0)
                    {
                        cardTexts[i].text = myCards[i].ToString();
                    }
                    else
                    {
                        cardTexts[i].text = "empty";
                    }
                }
            }
        }
    }
}
