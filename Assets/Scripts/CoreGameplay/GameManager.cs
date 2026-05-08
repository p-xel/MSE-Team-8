using Fusion;
using UnityEngine;
using System.Collections.Generic;

public enum GamePhase { Lobby, Playing, GameOver }

public class GameManager : NetworkBehaviour, IPlayerLeft
{
    [Networked] public GamePhase phase { get; set; }

    [Networked, Capacity(8)]
    public NetworkArray<PlayerRef> turnOrder { get; }

    [Networked] public int playerCount { get; set; }
    [Networked] public int currentTurnIndex { get; set; }

    public GameDeck gameDeck;
    public TableHand tableHand;

    public bool isGameStarted => phase == GamePhase.Playing;

    public override void Spawned()
    {
        if (gameDeck == null) gameDeck = GetComponent<GameDeck>();
        if (tableHand == null) tableHand = GetComponent<TableHand>();
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (Object.HasStateAuthority && phase == GamePhase.Lobby && Input.GetKeyDown(KeyCode.P))
            startGame();
    }

    private void startGame()
    {
        gameDeck.initDeck();
        tableHand.initialize(gameDeck);

        List<PlayerRef> activePlayers = new List<PlayerRef>();
        foreach (PlayerRef player in Runner.ActivePlayers)
            activePlayers.Add(player);

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef temp = activePlayers[i];
            int randomIndex = Random.Range(i, activePlayers.Count);
            activePlayers[i] = activePlayers[randomIndex];
            activePlayers[randomIndex] = temp;
        }

        playerCount = activePlayers.Count;
        for (int i = 0; i < playerCount; i++)
            turnOrder.Set(i, activePlayers[i]);

        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;
            for (int i = 0; i < playerCount; i++)
            {
                if (turnOrder[i] != hand.Object.StateAuthority) continue;
                CardData c1 = gameDeck.drawCard();
                CardData c2 = gameDeck.drawCard();
                CardData c3 = gameDeck.drawCard();
                if (hand.Object.HasStateAuthority)
                {
                    hand.myCards.Set(0, c1);
                    hand.myCards.Set(1, c2);
                    hand.myCards.Set(2, c3);
                }
                else
                {
                    hand.Rpc_ReceiveInitialHand(c1, c2, c3);
                }
                break;
            }
        }

        currentTurnIndex = 0;
        phase = GamePhase.Playing;
    }

    void IPlayerLeft.PlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority || phase != GamePhase.Playing) return;

        int leavingIndex = -1;
        for (int i = 0; i < playerCount; i++)
        {
            if (turnOrder[i] == player) { leavingIndex = i; break; }
        }

        if (leavingIndex == -1) return;

        for (int i = leavingIndex; i < playerCount - 1; i++)
            turnOrder.Set(i, turnOrder[i + 1]);
        turnOrder.Set(playerCount - 1, default);
        playerCount--;

        if (playerCount == 0) { phase = GamePhase.GameOver; return; }

        if (leavingIndex < currentTurnIndex)
            currentTurnIndex--;
        else if (leavingIndex == currentTurnIndex)
            currentTurnIndex = currentTurnIndex % playerCount;
    }

    public void endTurn()
    {
        if (playerCount > 0)
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_EndTurn()
    {
        endTurn();
    }

    public bool isPlayersTurn(PlayerRef player)
    {
        if (phase != GamePhase.Playing || playerCount == 0) return false;
        return turnOrder[currentTurnIndex] == player;
    }
}
