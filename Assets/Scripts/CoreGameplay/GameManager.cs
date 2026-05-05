using Fusion;
using UnityEngine;
using System.Collections.Generic;

// manages the game state, lobby, and turn order
public class GameManager : NetworkBehaviour
{
    // true when the game has started
    [Networked] public bool isGameStarted { get; set; }
    
    // holds the randomized order of players
    [Networked, Capacity(8)]
    public NetworkArray<PlayerRef> turnOrder { get; }
    
    // how many players are in the game
    [Networked] public int playerCount { get; set; }
    
    // the index in turnOrder of the current active player
    [Networked] public int currentTurnIndex { get; set; }

    // references to the deck and table, assigned in inspector or automatically if on same object
    public GameDeck gameDeck;
    public TableHand tableHand;

    public override void Spawned()
    {
        // automatically grab references if they are on the same gameobject
        if (gameDeck == null) gameDeck = GetComponent<GameDeck>();
        if (tableHand == null) tableHand = GetComponent<TableHand>();
    }

    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // master client (state authority) listens for 'p' to start the game
        if (Object.HasStateAuthority && !isGameStarted && Input.GetKeyDown(KeyCode.P))
        {
            startGame();
        }
    }

    private void startGame()
    {
        isGameStarted = true;

        // initialize deck and table
        if (gameDeck != null) gameDeck.initDeck();
        if (tableHand != null && gameDeck != null) tableHand.initialize(gameDeck);

        // collect all active players
        List<PlayerRef> activePlayers = new List<PlayerRef>();
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            activePlayers.Add(player);
        }

        // shuffle the players to randomize turn order
        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef temp = activePlayers[i];
            int randomIndex = Random.Range(i, activePlayers.Count);
            activePlayers[i] = activePlayers[randomIndex];
            activePlayers[randomIndex] = temp;
        }

        // populate the networked array
        playerCount = activePlayers.Count;
        for (int i = 0; i < playerCount; i++)
        {
            turnOrder.Set(i, activePlayers[i]);
        }

        currentTurnIndex = 0;
        Debug.Log($"game started with {playerCount} players!");
    }

    // called by a player when they finish their turn action
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_EndTurn()
    {
        if (playerCount > 0)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            Debug.Log($"turn ended. next turn is for player at index: {currentTurnIndex}");
        }
    }

    // helper to check if it's currently a specific player's turn
    public bool isPlayersTurn(PlayerRef player)
    {
        if (!isGameStarted || playerCount == 0) return false;
        return turnOrder[currentTurnIndex] == player;
    }
}
