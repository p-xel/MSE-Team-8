using Fusion;
using UnityEngine;

public class PlayerHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> myCards { get; }

    public PlayerHandUI playerHandUI;

    private GameManager gameManager;
    private GameDeck gameDeck;

    public static PlayerHand localHand { get; private set; }
    private int selectedMyCardIndex = -1;

    public int selectedCardIndex => selectedMyCardIndex;

    public override void Spawned()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        gameDeck = FindAnyObjectByType<GameDeck>();

        if (Object.HasStateAuthority)
            localHand = this;

        playerHandUI?.SetVisible(Object.HasStateAuthority);
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
        if (manager.Object.HasStateAuthority) manager.endTurn(Object.StateAuthority, myCards[0], myCards[1], myCards[2]);
        else manager.Rpc_EndTurn(Object.StateAuthority, myCards[0], myCards[1], myCards[2]);
    }

    public void swapCard(TableHand table, int myCardIndex, int tableCardIndex, GameManager manager)
    {
        if (!Object.HasStateAuthority) return;
        if (!manager.isPlayersTurn(Object.StateAuthority)) return;
        if (isHandEmpty()) return;

        CardData cardToSwap = myCards[myCardIndex];
        CardData incoming = table.tableCards[tableCardIndex];
        CardData c0 = myCardIndex == 0 ? incoming : myCards[0];
        CardData c1 = myCardIndex == 1 ? incoming : myCards[1];
        CardData c2 = myCardIndex == 2 ? incoming : myCards[2];

        if (table.Object.HasStateAuthority) table.performSwap(tableCardIndex, cardToSwap, Id, myCardIndex);
        else table.Rpc_SwapCard(tableCardIndex, cardToSwap, Id, myCardIndex);
        if (manager.Object.HasStateAuthority) manager.endTurn(Object.StateAuthority, c0, c1, c2);
        else manager.Rpc_EndTurn(Object.StateAuthority, c0, c1, c2);
    }

    public void skipTurn()
    {
        if (!Object.HasStateAuthority || gameManager == null) return;
        if (!gameManager.isPlayersTurn(Object.StateAuthority)) return;
        selectedMyCardIndex = -1;
        if (gameManager.Object.HasStateAuthority) gameManager.endTurn(Object.StateAuthority, myCards[0], myCards[1], myCards[2]);
        else gameManager.Rpc_EndTurn(Object.StateAuthority, myCards[0], myCards[1], myCards[2]);
    }

    public void knockTurn()
    {
        if (!Object.HasStateAuthority || gameManager == null) return;
        if (!gameManager.isPlayersTurn(Object.StateAuthority)) return;
        if (gameManager.Object.HasStateAuthority) gameManager.knock();
        else gameManager.Rpc_Knock();
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_ReceiveNewHand(CardData c0, CardData c1, CardData c2)
    {
        myCards.Set(0, c0);
        myCards.Set(1, c1);
        myCards.Set(2, c2);
    }

    public void stealTable()
    {
        if (!Object.HasStateAuthority || gameManager == null) return;
        if (!gameManager.isPlayersTurn(Object.StateAuthority) || isHandEmpty()) return;

        TableHand table = FindAnyObjectByType<TableHand>();
        if (table == null) return;

        CardData c0 = table.tableCards[0], c1 = table.tableCards[1], c2 = table.tableCards[2];

        if (table.Object.HasStateAuthority) table.stealAll(Id);
        else table.Rpc_StealAll(Id);

        selectedMyCardIndex = -1;
        if (gameManager.Object.HasStateAuthority) gameManager.endTurn(Object.StateAuthority, c0, c1, c2);
        else gameManager.Rpc_EndTurn(Object.StateAuthority, c0, c1, c2);
    }

    public override void Render()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
    }
}
