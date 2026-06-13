using Fusion;
using UnityEngine;
using System.Collections.Generic;
using CoreGameplay;

public enum GameState { Lobby, Playing, GameOver }

public class GameManager : NetworkBehaviour, IPlayerLeft
{
    [Networked] public GameState currentState { get; set; }
    [Networked] public NetworkBehaviourId matchWinnerHandId { get; set; }

    [Networked] public RoundPhase phase { get; set; }

    [Networked, Capacity(8)]
    public NetworkArray<NetworkBehaviourId> turnOrder { get; }

    [Networked] public int playerCount { get; set; }
    [Networked] public int currentTurnIndex { get; set; }
    [Networked] public int turnsLeftOnLastRound { get; set; }
    [Networked] public int roundCount { get; set; }
    [Networked] public int turnCycleCount { get; set; }
    [Networked] public PlayerRef winner { get; set; }
    [Networked] public NetworkBehaviourId winnerHand { get; set; }
    [Networked] public int consecutiveSkips { get; set; }

    [Networked] public NetworkBehaviourId currentWinnerHandId { get; set; }
    [Networked] public NetworkBehaviourId shotTargetHandId { get; set; }
    [Networked] public float cooldownTimer { get; set; }

    public GameDeck gameDeck;
    public TableHand tableHand;

    private NetworkBehaviourId _cardOverride = default;
    private CardData _co0, _co1, _co2;
    private float _aiShootTimer = 0f;

    private GameState _lastLoggedState = (GameState)(-1);
    private RoundPhase _lastLoggedPhase = (RoundPhase)(-1);

    public bool isRoundActive => phase == RoundPhase.Playing || phase == RoundPhase.LastRound;

    public override void Spawned()
    {
        if (gameDeck == null) gameDeck = FindAnyObjectByType<GameDeck>();
        if (tableHand == null) tableHand = FindAnyObjectByType<TableHand>();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid) return;

        if (currentState != _lastLoggedState || phase != _lastLoggedPhase)
        {
            _lastLoggedState = currentState;
            _lastLoggedPhase = phase;
            Debug.Log($"[GameManager] State: {currentState}, RoundManager Phase: {phase}, HasAuthority: {Object.HasStateAuthority}");
        }

        if (Object.HasStateAuthority)
        {
            if (phase == RoundPhase.Shooting)
            {
                if (Runner.TryFindBehaviour(currentWinnerHandId, out PlayerHand winnerHand))
                {
                    if (winnerHand.Object.InputAuthority == PlayerRef.None)
                    {
                        _aiShootTimer -= Runner.DeltaTime;
                        if (_aiShootTimer <= 0f)
                        {
                            PerformAIShoot(winnerHand);
                        }
                    }
                }
            }
            else if (phase == RoundPhase.Cooldown)
            {
                cooldownTimer -= Runner.DeltaTime;
            }

            if (currentState == GameState.Playing)
            {
                if (phase == RoundPhase.Cooldown && cooldownTimer <= 0f)
                {
                    if (CheckGameOver(out NetworkBehaviourId matchWinner))
                    {
                        matchWinnerHandId = matchWinner;
                        Debug.Log($"[GameManager] Game Over! The overall match winner is Hand ID: {matchWinner}!");
                        currentState = GameState.GameOver;
                        phase = RoundPhase.Inactive;
                    }
                    else
                    {
                        startRound();
                    }
                }
            }
        }
    }

    void IPlayerLeft.PlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority || phase != RoundPhase.Playing) return;

        int leavingIndex = -1;
        for (int i = 0; i < playerCount; i++)
        {
            if (Runner.TryFindBehaviour(turnOrder[i], out PlayerHand hand))
            {
                if (hand != null && hand.Object.StateAuthority == player)
                {
                    leavingIndex = i;
                    break;
                }
            }
        }

        if (leavingIndex == -1) return;

        for (int i = leavingIndex; i < playerCount - 1; i++)
            turnOrder.Set(i, turnOrder[i + 1]);
        turnOrder.Set(playerCount - 1, default);
        playerCount--;

        if (playerCount == 0)
        {
            phase = RoundPhase.Cooldown;
            cooldownTimer = 3f;
            return;
        }

        if (leavingIndex < currentTurnIndex)
            currentTurnIndex--;
        else if (leavingIndex == currentTurnIndex)
            currentTurnIndex = currentTurnIndex % playerCount;
    }

    public void startRound()
    {
        Debug.Log($"[GameManager] startRound() called. gameDeck is null: {gameDeck == null}, tableHand is null: {tableHand == null}");
        if (gameDeck == null) gameDeck = FindAnyObjectByType<GameDeck>();
        if (tableHand == null) tableHand = FindAnyObjectByType<TableHand>();

        Debug.Log($"[GameManager] startRound() resolved references. gameDeck is null: {gameDeck == null}, tableHand is null: {tableHand == null}");
        if (gameDeck == null || tableHand == null) return;

        gameDeck.initDeck();
        tableHand.initialize(gameDeck);

        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        List<PlayerHand> activeHands = new List<PlayerHand>();
        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid)
            {
                PlayerStatus status = hand.GetComponent<PlayerStatus>();
                if (status != null && status.lives > 0)
                {
                    activeHands.Add(hand);
                }
            }
        }

        for (int i = 0; i < activeHands.Count; i++)
        {
            PlayerHand temp = activeHands[i];
            int randomIndex = Random.Range(i, activeHands.Count);
            activeHands[i] = activeHands[randomIndex];
            activeHands[randomIndex] = temp;
        }

        playerCount = activeHands.Count;
        for (int i = 0; i < playerCount; i++)
            turnOrder.Set(i, activeHands[i].Id);

        List<PlayerHand> winnersWith31AtStart = new List<PlayerHand>();
        for (int i = 0; i < playerCount; i++)
        {
            PlayerHand hand = activeHands[i];
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

            if (Mathf.Approximately(computeHandValue(c1, c2, c3), 31f))
            {
                winnersWith31AtStart.Add(hand);
            }
        }

        roundCount++;
        currentTurnIndex = 0;
        turnCycleCount = 1;
        turnsLeftOnLastRound = 0;
        consecutiveSkips = 0;
        winner = default;
        winnerHand = default;
        currentWinnerHandId = default;
        shotTargetHandId = default;
        cooldownTimer = 0f;

        if (winnersWith31AtStart.Count > 0)
        {
            if (winnersWith31AtStart.Count == 1)
            {
                winner = winnersWith31AtStart[0].Object.StateAuthority;
                winnerHand = winnersWith31AtStart[0].Id;
                currentWinnerHandId = winnerHand;
                _aiShootTimer = 2f;
                phase = RoundPhase.Shooting;
                Debug.Log($"[GameManager] Start Round #{roundCount} - PlayerRef {winner} (Hand ID: {winnerHand}) was dealt exactly 31! Initiating shooting.");
            }
            else
            {
                winner = PlayerRef.None;
                winnerHand = default;
                phase = RoundPhase.Cooldown;
                cooldownTimer = 3f;
                Debug.Log($"[GameManager] Start Round #{roundCount} - Multiple players were dealt 31! Ending round in a tie.");
            }
        }
        else
        {
            phase = RoundPhase.Playing;
            Debug.Log($"[GameManager] Start Round #{roundCount} - Turn order established. Starting turn with player ID: {turnOrder[0]}");
        }
    }

    public void endTurn(NetworkBehaviourId actingHand, CardData c0, CardData c1, CardData c2, bool wasSkip)
    {
        Debug.Log($"[GameManager] Player hand {actingHand} ended their turn. Cards: [{c0.number}:{c0.color}] [{c1.number}:{c1.color}] [{c2.number}:{c2.color}]");
        _cardOverride = actingHand;
        _co0 = c0; _co1 = c1; _co2 = c2;
        if (wasSkip)
        {
            consecutiveSkips++;
            if (playerCount > 0 && consecutiveSkips >= playerCount)
            {
                if (tableHand == null) tableHand = FindAnyObjectByType<TableHand>();
                if (gameDeck == null) gameDeck = FindAnyObjectByType<GameDeck>();
                tableHand.initialize(gameDeck);
                consecutiveSkips = 0;
            }
        }
        else
        {
            consecutiveSkips = 0;
        }

        float actingHandValue = computeHandValue(c0, c1, c2);
        if (Mathf.Approximately(actingHandValue, 31f))
        {
            Debug.Log($"[GameManager] Player hand {actingHand} ended turn with exactly 31! Ending round immediately.");
            winner = Runner.TryFindBehaviour(actingHand, out PlayerHand hand) ? hand.Object.StateAuthority : PlayerRef.None;
            winnerHand = actingHand;
            currentWinnerHandId = winnerHand;
            _cardOverride = default;
            _aiShootTimer = 2f;
            phase = RoundPhase.Shooting;
            return;
        }

        if (phase == RoundPhase.LastRound)
        {
            turnsLeftOnLastRound--;
            Debug.Log($"[GameManager] Turn taken in Last Round. Turns left on last round: {turnsLeftOnLastRound}");
            if (turnsLeftOnLastRound <= 0)
            {
                Debug.Log($"[GameManager] Last Round ends. Determining round winner...");
                setWinner();
                if (winnerHand != default)
                {
                    currentWinnerHandId = winnerHand;
                    _aiShootTimer = 2f;
                    phase = RoundPhase.Shooting;
                }
                else
                {
                    phase = RoundPhase.Cooldown;
                    cooldownTimer = 3f;
                }
                return;
            }
        }

        if (playerCount > 0)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            if (currentTurnIndex == 0)
            {
                turnCycleCount++;
                Debug.Log($"[GameManager] Turn cycle ended. Starting Turn Cycle {turnCycleCount}");
            }
            Debug.Log($"[GameManager] Next turn: player ID {turnOrder[currentTurnIndex]} (Turn Index: {currentTurnIndex})");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_EndTurn(NetworkBehaviourId actingHand, CardData c0, CardData c1, CardData c2, bool wasSkip)
    {
        endTurn(actingHand, c0, c1, c2, wasSkip);
    }

    public void knock()
    {
        turnsLeftOnLastRound = playerCount - 1;
        consecutiveSkips = 0;
        Debug.Log($"[GameManager] Knock called by Player ID {turnOrder[currentTurnIndex]}! Last round initiated. Each remaining player gets 1 more turn. (Turns left: {turnsLeftOnLastRound})");
        if (turnsLeftOnLastRound <= 0)
        {
            Debug.Log($"[GameManager] No remaining players to take turns after knock. Determining winner...");
            setWinner();
            if (winnerHand != default)
            {
                currentWinnerHandId = winnerHand;
                _aiShootTimer = 2f;
                phase = RoundPhase.Shooting;
            }
            else
            {
                phase = RoundPhase.Cooldown;
                cooldownTimer = 3f;
            }
            return;
        }
        phase = RoundPhase.LastRound;
        if (playerCount > 0)
        {
            currentTurnIndex = (currentTurnIndex + 1) % playerCount;
            Debug.Log($"[GameManager] Next turn: player ID {turnOrder[currentTurnIndex]} (Turn Index: {currentTurnIndex})");
        }
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
        NetworkBehaviourId bestHand = default;
        float best = -1f;
        bool tied = false;

        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;
            PlayerStatus status = hand.GetComponent<PlayerStatus>();
            if (status != null && status.lives <= 0) continue;
            float v = hand.Id == _cardOverride
                ? computeHandValue(_co0, _co1, _co2)
                : computeHandValue(hand.myCards[0], hand.myCards[1], hand.myCards[2]);
            if (v > best) { best = v; bestPlayer = hand.Object.StateAuthority; bestHand = hand.Id; tied = false; }
            else if (v == best) tied = true;
        }
        _cardOverride = default;
        winner = tied ? PlayerRef.None : bestPlayer;
        winnerHand = tied ? default : bestHand;

        if (tied)
        {
            Debug.Log($"[GameManager] Round Over! Tied round at hand value {best}. No single winner.");
        }
        else
        {
            Debug.Log($"[GameManager] Round Over! Winner of this round is PlayerRef {winner} (Hand ID: {winnerHand}) with best hand value of {best}");
            if (Runner.TryFindBehaviour(winnerHand, out PlayerHand winnerHandObj))
            {
                PlayerStatus winnerStatus = winnerHandObj.GetComponent<PlayerStatus>();
                if (winnerStatus != null) winnerStatus.roundsWon++;
            }
        }
    }

    private float computeHandValue(CardData c0, CardData c1, CardData c2)
    {
        // Three of a kind (same rank) valued at 30.5 points.
        if (c0.number > 0 && c0.number == c1.number && c1.number == c2.number)
            return 30.5f;
        // Otherwise, score is the sum of cards of the same suit, we return the highest suit total.
        int[] totals = new int[4];
        foreach (CardData c in new[] { c0, c1, c2 })
            if (c.number > 0) totals[(int)c.color] += c.gameValue;
        float best = 0;
        for (int i = 0; i < 4; i++) if (totals[i] > best) best = totals[i];
        return best;
    }

    public bool isPlayersTurn(PlayerRef player)
    {
        if (phase != RoundPhase.Playing && phase != RoundPhase.LastRound) return false;
        if (playerCount == 0) return false;
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        foreach (var hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid && hand.Object.StateAuthority == player)
            {
                return isPlayersTurn(hand.Id);
            }
        }
        return false;
    }

    public bool isPlayersTurn(NetworkBehaviourId handId)
    {
        if (phase != RoundPhase.Playing && phase != RoundPhase.LastRound) return false;
        if (playerCount == 0) return false;
        return turnOrder[currentTurnIndex] == handId;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ChooseTarget(NetworkBehaviourId targetHandId)
    {
        if (phase != RoundPhase.Shooting) return;
        if (targetHandId == currentWinnerHandId) return;

        if (Runner.TryFindBehaviour(targetHandId, out PlayerHand targetHand))
        {
            PlayerStatus targetStatus = targetHand.GetComponent<PlayerStatus>();
            if (targetStatus != null && targetStatus.lives > 0)
            {
                float targetValue = computeHandValue(targetHand.myCards[0], targetHand.myCards[1], targetHand.myCards[2]);
                // If target has three of a kind (30.5 points), they deflect the shot.
                bool deflected = Mathf.Approximately(targetValue, 30.5f);

                if (deflected)
                {
                    targetStatus.deflects++;
                    Debug.Log($"[GameManager] Target hand {targetHandId} deflected the shot with 30.5!");
                }
                else
                {
                    targetStatus.lives--;
                    Debug.Log($"[GameManager] Winner hand {currentWinnerHandId} shot target hand {targetHandId}! Lives remaining for target: {targetStatus.lives}");
                    if (Runner.TryFindBehaviour(currentWinnerHandId, out PlayerHand shooterHand))
                    {
                        PlayerStatus shooterStatus = shooterHand.GetComponent<PlayerStatus>();
                        if (shooterStatus != null) shooterStatus.kills++;
                    }
                }

                shotTargetHandId = targetHandId;
                phase = RoundPhase.Cooldown;
                cooldownTimer = 3f;
            }
        }
    }

    private void PerformAIShoot(PlayerHand AIHand)
    {
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        List<PlayerHand> validTargets = new List<PlayerHand>();
        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid && hand.Id != AIHand.Id)
            {
                PlayerStatus status = hand.GetComponent<PlayerStatus>();
                if (status != null && status.lives > 0)
                {
                    validTargets.Add(hand);
                }
            }
        }

        if (validTargets.Count > 0)
        {
            PlayerHand randomTarget = validTargets[Random.Range(0, validTargets.Count)];
            Rpc_ChooseTarget(randomTarget.Id);
        }
        else
        {
            phase = RoundPhase.Cooldown;
            cooldownTimer = 3f;
        }
    }

    public PlayerHand GetLookTarget(PlayerHand shooterHand)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        PlayerHand bestTarget = null;
        float bestDot = 0.5f;

        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object == null || !hand.Object.IsValid || hand.Id == shooterHand.Id)
                continue;

            PlayerStatus status = hand.GetComponent<PlayerStatus>();
            if (status == null || status.lives <= 0)
                continue;

            // Calculate dot product between camera forward and direction to other players
            // to find the player closest to the screen center look direction.
            Vector3 toTarget = (hand.transform.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, toTarget);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestTarget = hand;
            }
        }

        return bestTarget;
    }

    private bool CheckGameOver(out NetworkBehaviourId matchWinner)
    {
        matchWinner = default;
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        List<PlayerHand> aliveHands = new List<PlayerHand>();

        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid)
            {
                PlayerStatus status = hand.GetComponent<PlayerStatus>();
                if (status != null && status.lives > 0)
                {
                    aliveHands.Add(hand);
                }
            }
        }

        if (aliveHands.Count == 1)
        {
            matchWinner = aliveHands[0].Id;
            return true;
        }

        return aliveHands.Count == 0;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_StartGame()
    {
        Debug.Log($"[GameManager] Rpc_StartGame received. currentState: {currentState}");
        if (currentState == GameState.Lobby || currentState == GameState.GameOver)
        {
            PlayerStatus[] allStatuses = FindObjectsByType<PlayerStatus>();
            Debug.Log($"[GameManager] Rpc_StartGame: Found {allStatuses.Length} PlayerStatuses. Setting lives to {PlayerStatus.MaxLives}.");
            foreach (PlayerStatus status in allStatuses)
            {
                if (status.Object != null && status.Object.IsValid)
                {
                    status.lives = PlayerStatus.MaxLives;
                }
            }
            currentState = GameState.Playing;
            startRound();
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[GameManager] Key P pressed. Object is null: {Object == null}, Object IsValid: {(Object != null && Object.IsValid)}, currentState: {currentState}");
            Rpc_StartGame();
        }

        if (phase == RoundPhase.Shooting)
        {
            if (Runner.TryFindBehaviour(currentWinnerHandId, out PlayerHand winnerHand))
            {
                if (winnerHand.Object != null && winnerHand.Object.HasInputAuthority)
                {
                    PlayerHand target = GetLookTarget(winnerHand);
                    if (target != null && Input.GetKeyDown(KeyCode.Space))
                    {
                        Rpc_ChooseTarget(target.Id);
                    }
                }
            }
        }
    }
}
