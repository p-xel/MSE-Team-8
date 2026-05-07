using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Networked] public bool isGameStarted { get; set; }

    // randomized turn order, populated in startGame()
    [Networked, Capacity(8)]
    public NetworkArray<PlayerRef> turnOrder { get; }

    [Networked] public int playerCount { get; set; }

    // index into turnOrder of the active player
    [Networked] public int currentTurnIndex { get; set; }

    // assigned in inspector, or auto-resolved if on the same GameObject
    public GameDeck gameDeck;
    public TableHand tableHand;

    public override void Spawned()
    {
        if (gameDeck == null) gameDeck = GetComponent<GameDeck>();
        if (tableHand == null) tableHand = GetComponent<TableHand>();
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // only the state authority (host) can start the game; P is the host hotkey
        if (Object.HasStateAuthority && !isGameStarted && Input.GetKeyDown(KeyCode.P))
        {
            startGame();
        }
    }

    private void startGame()
    {
        isGameStarted = true;

        if (gameDeck != null) gameDeck.initDeck();
        if (tableHand != null && gameDeck != null) tableHand.initialize(gameDeck);

        List<PlayerRef> activePlayers = new List<PlayerRef>();
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            activePlayers.Add(player);
        }

        // shuffle of turn order
        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef temp = activePlayers[i];
            int randomIndex = Random.Range(i, activePlayers.Count);
            activePlayers[i] = activePlayers[randomIndex];
            activePlayers[randomIndex] = temp;
        }

        playerCount = activePlayers.Count;
        for (int i = 0; i < playerCount; i++)
        {
            turnOrder.Set(i, activePlayers[i]);
        }

        currentTurnIndex = 0;
        Debug.Log($"game started with {playerCount} players!");
    }

    // called by any client when their turn action completes
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_EndTurn()
    {
        if (playerCount > 0)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            Debug.Log($"turn ended. next turn is for player at index: {currentTurnIndex}");
        }
    }

    public bool isPlayersTurn(PlayerRef player)
    {
        if (!isGameStarted || playerCount == 0) return false;
        return turnOrder[currentTurnIndex] == player;
    }
}
