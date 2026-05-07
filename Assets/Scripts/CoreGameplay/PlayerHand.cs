using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> myCards { get; }

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

        // isolate overlay ui to the currently-viewed peer in multi-peer mode
        if (playerHandCanvas != null)
        {
            playerHandCanvas.enabled = Runner.GetVisible();
        }

        // only the locally-viewed peer reads keyboard input (multi-peer safe)
        if (Object.HasStateAuthority && gameManager != null && Runner.GetVisible())
        {
            if (gameManager.isPlayersTurn(Object.StateAuthority))
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    Debug.Log("skipping turn.");
                    selectedMyCardIndex = -1;
                    gameManager.Rpc_EndTurn();
                }
            }
        }
    }

    public bool isHandEmpty()
    {
        return myCards[0].number == 0 && myCards[1].number == 0 && myCards[2].number == 0;
    }

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

    // wired to UI Button onClick
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

    // wired to UI Button onClick; consumes the previously-selected hand card to swap
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
                    selectedMyCardIndex = -1;
                }
            }
            else
            {
                Debug.LogWarning("select a card from your hand first!");
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ReceiveInitialHand(CardData c1, CardData c2, CardData c3)
    {
        myCards.Set(0, c1);
        myCards.Set(1, c2);
        myCards.Set(2, c3);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ReceiveSwappedCard(CardData newCard, int myCardIndex)
    {
        myCards.Set(myCardIndex, newCard);
    }

    public override void Render()
    {
        // first turn: auto-draw 3 cards then end turn
        if (Object.HasStateAuthority && gameManager != null && gameDeck != null)
        {
            if (gameManager.isPlayersTurn(Object.StateAuthority) && isHandEmpty() && !hasRequestedInitialCards)
            {
                hasRequestedInitialCards = true;
                draw3Cards(gameDeck, gameManager);
            }
        }

        // only the local player updates their own hand UI
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
