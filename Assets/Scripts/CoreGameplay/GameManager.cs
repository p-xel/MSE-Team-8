using Fusion;
using UnityEngine;
using System.Collections.Generic;

public enum GamePhase { Lobby, Playing, LastRound, DealOver }

public class GameManager : NetworkBehaviour, IPlayerLeft
{
    [Networked] public GamePhase phase { get; set; }

    [Networked, Capacity(8)]
    public NetworkArray<PlayerRef> turnOrder { get; }

    [Networked] public int playerCount { get; set; }
    [Networked] public int currentTurnIndex { get; set; }
    [Networked] public int turnsLeftOnLastRound { get; set; }
    [Networked] public int dealCount { get; set; }
    [Networked] public int roundCount { get; set; }
    [Networked] public PlayerRef winner { get; set; }

    public GameDeck gameDeck;
    public TableHand tableHand;

    private PlayerRef _cardOverride = PlayerRef.None;
    private CardData _co0, _co1, _co2;

    public bool isDealActive => phase == GamePhase.Playing || phase == GamePhase.LastRound;

    public override void Spawned()
    {
        if (gameDeck == null) gameDeck = GetComponent<GameDeck>();
        if (tableHand == null) tableHand = GetComponent<TableHand>();
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (Object.HasStateAuthority && Input.GetKeyDown(KeyCode.P) && (phase == GamePhase.Lobby || phase == GamePhase.DealOver))
            startDeal();
    }

    private void startDeal()
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

        dealCount++;
        currentTurnIndex = 0;
        roundCount = 1;
        turnsLeftOnLastRound = 0;
        winner = default;
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

        if (playerCount == 0) { phase = GamePhase.DealOver; return; }

        if (leavingIndex < currentTurnIndex)
            currentTurnIndex--;
        else if (leavingIndex == currentTurnIndex)
            currentTurnIndex = currentTurnIndex % playerCount;
    }

    public void endTurn(PlayerRef actingPlayer, CardData c0, CardData c1, CardData c2)
    {
        _cardOverride = actingPlayer;
        _co0 = c0; _co1 = c1; _co2 = c2;
        if (phase == GamePhase.LastRound)
        {
            turnsLeftOnLastRound--;
            if (turnsLeftOnLastRound <= 0) { setWinner(); phase = GamePhase.DealOver; return; }
        }
        if (playerCount > 0)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            if (currentTurnIndex == 0) roundCount++;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_EndTurn(PlayerRef actingPlayer, CardData c0, CardData c1, CardData c2)
    {
        endTurn(actingPlayer, c0, c1, c2);
    }

    public void knock()
    {
        turnsLeftOnLastRound = playerCount - 1;
        if (turnsLeftOnLastRound <= 0) { setWinner(); phase = GamePhase.DealOver; return; }
        phase = GamePhase.LastRound;
        if (playerCount > 0)
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_Knock()
    {
        knock();
    }

    private void setWinner()
    {
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        PlayerRef bestPlayer = PlayerRef.None;
        float best = -1f;
        bool tied = false;

        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;
            float v = hand.Object.StateAuthority == _cardOverride
                ? computeHandValue(_co0, _co1, _co2)
                : computeHandValue(hand.myCards[0], hand.myCards[1], hand.myCards[2]);
            if (v > best) { best = v; bestPlayer = hand.Object.StateAuthority; tied = false; }
            else if (v == best) tied = true;
        }
        _cardOverride = PlayerRef.None;
        winner = tied ? PlayerRef.None : bestPlayer;
    }

    private float computeHandValue(CardData c0, CardData c1, CardData c2)
    {
        if (c0.number > 0 && c0.number == c1.number && c1.number == c2.number)
            return 30.5f;
        int[] totals = new int[4];
        foreach (CardData c in new[] { c0, c1, c2 })
            if (c.number > 0) totals[(int)c.color] += c.gameValue;
        float best = 0;
        for (int i = 0; i < 4; i++) if (totals[i] > best) best = totals[i];
        return best;
    }

    public bool isPlayersTurn(PlayerRef player)
    {
        if (phase != GamePhase.Playing && phase != GamePhase.LastRound) return false;
        if (playerCount == 0) return false;
        return turnOrder[currentTurnIndex] == player;
    }
}
