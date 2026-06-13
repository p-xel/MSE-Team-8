using Fusion;
using UnityEngine;

public class PlayerHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> myCards { get; }

    public PlayerHandUI playerHandUI;

    private GameManager roundManager;
    private GameDeck gameDeck;
    private PlayerStatus playerStatus;

    public static PlayerHand localHand { get; private set; }
    public static readonly System.Collections.Generic.List<PlayerHand> ActiveHands = new System.Collections.Generic.List<PlayerHand>();
    private int selectedMyCardIndex = -1;

    public int selectedCardIndex => selectedMyCardIndex;

    private float aiTimer = 0f;
    private const float AIDelay = 1.5f;

    public override void Spawned()
    {
        ActiveHands.Add(this);
        roundManager = FindAnyObjectByType<GameManager>();
        gameDeck = FindAnyObjectByType<GameDeck>();

        if (Object.HasInputAuthority)
            localHand = this;

        playerHandUI?.SetVisible(Object.HasInputAuthority);
    }

    public override void Despawned(NetworkRunner runner, bool hasStateAuthority)
    {
        ActiveHands.Remove(this);
        if (localHand == this)
            localHand = null;
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (!Object.HasStateAuthority) return;

        if (playerStatus == null) playerStatus = GetComponent<PlayerStatus>();
        if (playerStatus != null && playerStatus.lives <= 0)
        {
            if (!isHandEmpty())
            {
                myCards.Set(0, default);
                myCards.Set(1, default);
                myCards.Set(2, default);
            }
            return;
        }

        if (Object.InputAuthority != PlayerRef.None) return;

        if (roundManager == null) roundManager = FindAnyObjectByType<GameManager>();
        if (roundManager == null || !roundManager.isPlayersTurn(Id))
        {
            aiTimer = 0f;
            return;
        }

        aiTimer += Time.deltaTime;
        if (aiTimer >= AIDelay)
        {
            aiTimer = 0f;
            PerformRandomAction();
        }
    }

    private void PerformRandomAction()
    {
        if (gameDeck == null) gameDeck = FindAnyObjectByType<GameDeck>();
        TableHand table = FindAnyObjectByType<TableHand>();
        if (table == null) return;

        if (isHandEmpty())
        {
            draw3Cards(gameDeck, roundManager);
            return;
        }

        float roll = Random.value;
        if (roll < 0.55f)
        {
            int myIndex = Random.Range(0, 3);
            int tableIndex = Random.Range(0, 3);
            swapCard(table, myIndex, tableIndex, roundManager);
        }
        else if (roll < 0.70f)
        {
            stealTable();
        }
        else if (roll < 0.85f)
        {
            knockTurn();
        }
        else
        {
            skipTurn();
        }
    }

    public bool isHandEmpty()
    {
        return myCards[0].number == 0 && myCards[1].number == 0 && myCards[2].number == 0;
    }

    public void draw3Cards(GameDeck deck, GameManager manager)
    {
        if (!Object.HasInputAuthority && !Object.HasStateAuthority) return;
        if (!manager.isPlayersTurn(Id)) return;
        if (!isHandEmpty()) return;
        deck.Rpc_Draw3CardsForPlayer(Id);
        if (manager.Object.HasStateAuthority) manager.endTurn(Id, myCards[0], myCards[1], myCards[2], false);
        else manager.Rpc_EndTurn(Id, myCards[0], myCards[1], myCards[2], false);
    }

    public void swapCard(TableHand table, int myCardIndex, int tableCardIndex, GameManager manager)
    {
        if (!Object.HasInputAuthority && !Object.HasStateAuthority) return;
        if (!manager.isPlayersTurn(Id)) return;
        if (isHandEmpty()) return;

        CardData cardToSwap = myCards[myCardIndex];
        CardData incoming = table.tableCards[tableCardIndex];
        CardData c0 = myCardIndex == 0 ? incoming : myCards[0];
        CardData c1 = myCardIndex == 1 ? incoming : myCards[1];
        CardData c2 = myCardIndex == 2 ? incoming : myCards[2];

        if (table.Object.HasStateAuthority) table.performSwap(tableCardIndex, cardToSwap, Id, myCardIndex);
        else table.Rpc_SwapCard(tableCardIndex, cardToSwap, Id, myCardIndex);
        if (manager.Object.HasStateAuthority) manager.endTurn(Id, c0, c1, c2, false);
        else manager.Rpc_EndTurn(Id, c0, c1, c2, false);
    }

    public void skipTurn()
    {
        if ((!Object.HasInputAuthority && !Object.HasStateAuthority) || roundManager == null) return;
        if (!roundManager.isPlayersTurn(Id)) return;
        selectedMyCardIndex = -1;
        if (roundManager.Object.HasStateAuthority) roundManager.endTurn(Id, myCards[0], myCards[1], myCards[2], true);
        else roundManager.Rpc_EndTurn(Id, myCards[0], myCards[1], myCards[2], true);
    }

    public void knockTurn()
    {
        if ((!Object.HasInputAuthority && !Object.HasStateAuthority) || roundManager == null) return;
        if (!roundManager.isPlayersTurn(Id)) return;
        if (roundManager.Object.HasStateAuthority) roundManager.knock();
        else roundManager.Rpc_Knock();
    }

    public void selectMyCard(int index)
    {
        if ((!Object.HasInputAuthority && !Object.HasStateAuthority) || roundManager == null) return;
        if (!roundManager.isPlayersTurn(Id) || isHandEmpty()) return;
        selectedMyCardIndex = index;
    }

    public void selectTableCard(int tableIndex)
    {
        if ((!Object.HasInputAuthority && !Object.HasStateAuthority) || roundManager == null) return;
        if (!roundManager.isPlayersTurn(Id)) return;
        if (selectedMyCardIndex == -1) return;

        TableHand table = FindAnyObjectByType<TableHand>();
        if (table == null) return;

        swapCard(table, selectedMyCardIndex, tableIndex, roundManager);
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
        if ((!Object.HasInputAuthority && !Object.HasStateAuthority) || roundManager == null) return;
        if (!roundManager.isPlayersTurn(Id) || isHandEmpty()) return;

        TableHand table = FindAnyObjectByType<TableHand>();
        if (table == null) return;

        CardData c0 = table.tableCards[0], c1 = table.tableCards[1], c2 = table.tableCards[2];

        if (table.Object.HasStateAuthority) table.stealAll(Id);
        else table.Rpc_StealAll(Id);

        selectedMyCardIndex = -1;
        if (roundManager.Object.HasStateAuthority) roundManager.endTurn(Id, c0, c1, c2, false);
        else roundManager.Rpc_EndTurn(Id, c0, c1, c2, false);
    }

    public override void Render()
    {
        if (roundManager == null) roundManager = FindAnyObjectByType<GameManager>();
    }
}
