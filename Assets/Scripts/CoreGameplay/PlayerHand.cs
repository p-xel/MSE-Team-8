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
    private TurnDisplayTMPro turnDisplay;

    public static PlayerHand localHand { get; private set; }
    private int selectedMyCardIndex = -1;

    public override void Spawned()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        gameDeck = FindAnyObjectByType<GameDeck>();
        turnDisplay = transform.root.GetComponentInChildren<TurnDisplayTMPro>();

        if (Object.HasStateAuthority)
        {
            localHand = this;
            if (playerHandCanvas != null) playerHandCanvas.enabled = true;
        }
        else
        {
            if (playerHandCanvas != null) playerHandCanvas.enabled = false;
        }
    }

    void Update()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority) return;
        if (gameManager == null) return;

        if (gameManager.isPlayersTurn(Object.StateAuthority) && Input.GetKeyDown(KeyCode.S))
        {
            selectedMyCardIndex = -1;
            if (gameManager.Object.HasStateAuthority) gameManager.endTurn();
            else gameManager.Rpc_EndTurn();
        }
    }

    public bool isHandEmpty()
    {
        return myCards[0].number == 0 && myCards[1].number == 0 && myCards[2].number == 0;
    }

    public void draw3Cards(GameDeck deck, GameManager manager)
    {
        if (!Object.HasStateAuthority) return;
        if (!manager.isPlayersTurn(Object.StateAuthority)) return;
        if (!isHandEmpty()) return;
        deck.Rpc_Draw3CardsForPlayer(Id);
        if (manager.Object.HasStateAuthority) manager.endTurn();
        else manager.Rpc_EndTurn();
    }

    public void swapCard(TableHand table, int myCardIndex, int tableCardIndex, GameManager manager)
    {
        if (!Object.HasStateAuthority) return;
        if (!manager.isPlayersTurn(Object.StateAuthority)) return;
        if (isHandEmpty()) return;

        CardData cardToSwap = myCards[myCardIndex];
        if (table.Object.HasStateAuthority) table.performSwap(tableCardIndex, cardToSwap, Id, myCardIndex);
        else table.Rpc_SwapCard(tableCardIndex, cardToSwap, Id, myCardIndex);
        if (manager.Object.HasStateAuthority) manager.endTurn();
        else manager.Rpc_EndTurn();
    }

    public void selectMyCard(int index)
    {
        if (!Object.HasStateAuthority || gameManager == null) return;
        if (!gameManager.isPlayersTurn(Object.StateAuthority) || isHandEmpty()) return;
        selectedMyCardIndex = index;
    }

    public void selectTableCard(int tableIndex)
    {
        if (!Object.HasStateAuthority || gameManager == null) return;
        if (!gameManager.isPlayersTurn(Object.StateAuthority)) return;
        if (selectedMyCardIndex == -1) return;

        TableHand table = FindAnyObjectByType<TableHand>();
        if (table == null) return;

        swapCard(table, selectedMyCardIndex, tableIndex, gameManager);
        selectedMyCardIndex = -1;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_ReceiveInitialHand(CardData c1, CardData c2, CardData c3)
    {
        myCards.Set(0, c1);
        myCards.Set(1, c2);
        myCards.Set(2, c3);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_ReceiveSwappedCard(CardData newCard, int myCardIndex)
    {
        myCards.Set(myCardIndex, newCard);
    }

    public override void Render()
    {
        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < 3; i++)
            {
                if (cardTexts == null || i >= cardTexts.Length || cardTexts[i] == null) continue;
                cardTexts[i].text = myCards[i].number > 0 ? myCards[i].ToString() : "empty";
            }
        }

        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (turnDisplay == null || gameManager == null) return;
        if (!Object.HasStateAuthority) return;

        string text = "";
        if (gameManager.phase == GamePhase.Playing)
            text = gameManager.isPlayersTurn(Object.StateAuthority) ? "your turn!" : "waiting...";
        turnDisplay.setText(text);
    }
}
