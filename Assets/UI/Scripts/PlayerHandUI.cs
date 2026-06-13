using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UIElements;
using CoreGameplay;

public class PlayerHandUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    private UIDocument doc;
    private PlayerHand playerHand;
    private GameManager gameManager;
    private GameManager roundManager;
    private PlayerStatus playerStatus;

    private readonly CardElement[] handCards = new CardElement[3];
    private GameButtonElement skipButton;
    private GameButtonElement knockButton;
    private GameButtonElement stealButton;
    private NotificationElement notification;
    private Label dealRoundLabel;
    private Label suitIconLabel;
    private Label handValueLabel;
    private readonly LifeBarElement[] lifeBars = new LifeBarElement[3];
    private VisualElement bottomContainer;
    private VisualElement handValueRow;
    private Label nameLabel;

    private VisualElement othersPanel;
    private readonly Dictionary<PlayerHand, OtherEntry> others = new Dictionary<PlayerHand, OtherEntry>();
    private readonly List<PlayerHand> othersStale = new List<PlayerHand>();

    private class OtherEntry
    {
        public VisualElement container;
        public Label name;
        public LifeBarElement health;
    }

    private RoundPhase lastPhase = RoundPhase.Inactive;
    private NetworkBehaviourId lastShotTargetId = default;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        playerHand = GetComponentInParent<PlayerHand>();
        BuildUI();
    }

    public void SetVisible(bool visible)
    {
        doc.enabled = visible;
        enabled = visible;
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        roundManager = gameManager;
        if (playerHand != null)
            playerStatus = playerHand.GetComponent<PlayerStatus>();
    }

    void BuildUI()
    {
        // Programmatically build UI Toolkit hierarchy, applying style sheets and setting default values.
        var root = doc.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        othersPanel = new VisualElement();
        othersPanel.AddToClassList("others-panel");
        othersPanel.pickingMode = PickingMode.Ignore;
        root.Add(othersPanel);

        var phRoot = new VisualElement();
        phRoot.AddToClassList("ph-root");
        phRoot.pickingMode = PickingMode.Ignore;

        var top = new VisualElement();
        top.AddToClassList("ph-top");
        top.pickingMode = PickingMode.Ignore;

        notification = new NotificationElement();
        phRoot.Add(notification);

        dealRoundLabel = new Label();
        dealRoundLabel.AddToClassList("ph-deal-label");
        dealRoundLabel.pickingMode = PickingMode.Ignore;
        top.Add(dealRoundLabel);

        handValueRow = new VisualElement();
        handValueRow.AddToClassList("ph-hand-value-row");
        handValueRow.pickingMode = PickingMode.Ignore;
        suitIconLabel = new Label("♠");
        suitIconLabel.AddToClassList("ph-hand-value-icon");
        suitIconLabel.pickingMode = PickingMode.Ignore;
        handValueLabel = new Label("0");
        handValueLabel.AddToClassList("ph-hand-value-label");
        handValueLabel.pickingMode = PickingMode.Ignore;
        handValueRow.Add(suitIconLabel);
        handValueRow.Add(handValueLabel);
        top.Add(handValueRow);

        phRoot.Add(top);

        bottomContainer = new VisualElement();
        bottomContainer.AddToClassList("ph-bottom");
        bottomContainer.pickingMode = PickingMode.Ignore;

        var cardsRow = new VisualElement();
        cardsRow.AddToClassList("ph-cards-row");
        cardsRow.pickingMode = PickingMode.Ignore;
        cardsRow.style.display = DisplayStyle.None;
        for (int i = 0; i < 3; i++)
        {
            handCards[i] = new CardElement();
            handCards[i].SetEmpty();
            int idx = i;
            handCards[i].RegisterCallback<ClickEvent>(_ => {
                AudioManager.PlayButtonClick();
                playerHand?.selectMyCard(idx);
            });
            cardsRow.Add(handCards[i]);
        }
        bottomContainer.Add(cardsRow);

        var actions = new VisualElement();
        actions.AddToClassList("ph-actions");
        actions.pickingMode = PickingMode.Ignore;

        stealButton = new GameButtonElement();
        stealButton.Setup(">> STEAL THE TABLE <<", GameButtonStyle.Special, () => playerHand?.stealTable());
        actions.Add(stealButton);

        knockButton = new GameButtonElement();
        knockButton.Setup(">> KNOCK <<", GameButtonStyle.Danger, () => playerHand?.knockTurn());
        actions.Add(knockButton);

        skipButton = new GameButtonElement();
        skipButton.Setup("SKIP >>>", GameButtonStyle.Primary, () => playerHand?.skipTurn());
        actions.Add(skipButton);

        bottomContainer.Add(actions);
        phRoot.Add(bottomContainer);

        var lifeSection = new VisualElement();
        lifeSection.AddToClassList("ph-life-section");
        lifeSection.pickingMode = PickingMode.Ignore;

        nameLabel = new Label("");
        nameLabel.AddToClassList("ph-life-title");
        nameLabel.pickingMode = PickingMode.Ignore;
        lifeSection.Add(nameLabel);

        var lifeBarsRow = new VisualElement();
        lifeBarsRow.AddToClassList("ph-life-bars");
        lifeBarsRow.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < 3; i++)
        {
            lifeBars[i] = new LifeBarElement();
            lifeBars[i].SetValue(1f);
            lifeBarsRow.Add(lifeBars[i]);
        }
        lifeSection.Add(lifeBarsRow);
        phRoot.Add(lifeSection);



        root.Add(phRoot);

        root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);
        foreach (var c in handCards) c.pickingMode = PickingMode.Position;
        stealButton.pickingMode = PickingMode.Position;
        knockButton.pickingMode = PickingMode.Position;
        skipButton.pickingMode = PickingMode.Position;
    }

    void Update()
    {
        UpdateOthers();

        if (playerHand == null || playerHand.Object == null || !playerHand.Object.IsValid) return;

        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (roundManager == null) roundManager = gameManager;

        if (roundManager != null && roundManager.Object != null && roundManager.Object.IsValid)
        {
            RoundPhase currentPhase = roundManager.phase;
            if (currentPhase != lastPhase)
            {
                if (currentPhase == RoundPhase.LastRound)
                {
                    AudioManager.PlayKnock();
                }
                lastPhase = currentPhase;
            }

            NetworkBehaviourId currentShotTarget = roundManager.shotTargetHandId;
            if (currentShotTarget != lastShotTargetId)
            {
                // Trigger gunshot sound effect when a player gets shot.
                if (currentShotTarget != default)
                {
                    AudioManager.PlayShoot();
                }
                lastShotTargetId = currentShotTarget;
            }
        }

        bool isShootingOrCooldown = roundManager != null && 
                                    roundManager.Object != null && 
                                    roundManager.Object.IsValid && 
                                    (roundManager.phase == RoundPhase.Shooting || 
                                     roundManager.phase == RoundPhase.Cooldown);

        if (bottomContainer != null)
            bottomContainer.style.display = isShootingOrCooldown ? DisplayStyle.None : DisplayStyle.Flex;
        if (handValueRow != null)
            handValueRow.style.display = isShootingOrCooldown ? DisplayStyle.None : DisplayStyle.Flex;

        RefreshCards();
        RefreshLabels();
        RefreshButtons();
        RefreshLifeBars();
    }

    void RefreshCards()
    {
        int selected = playerHand.selectedCardIndex;
        for (int i = 0; i < 3; i++)
        {
            CardData card = playerHand.myCards[i];
            handCards[i].style.opacity = card.number > 0 ? 1f : 0.25f;
            if (card.number > 0) handCards[i].SetCard(card);
            else handCards[i].SetEmpty();
            handCards[i].SetSelected(i == selected);
        }
    }

    void RefreshLabels()
    {
        if (roundManager == null || roundManager.Object == null || !roundManager.Object.IsValid) return;

        RoundPhase phase = roundManager.phase;
        if (phase == RoundPhase.Playing || phase == RoundPhase.LastRound)
        {
            string roundStr = phase == RoundPhase.LastRound ? "LAST ROUND" : $"TURN {roundManager.turnCycleCount}";
            dealRoundLabel.text = $"ROUND {roundManager.roundCount}\n{roundStr}";
        }
        else
        {
            dealRoundLabel.text = string.Empty;
        }

        var (val, symbol, suitClass) = ComputeHand();
        handValueLabel.text = val;
        suitIconLabel.text = symbol;
        foreach (string s in new[] { "spades", "hearts", "clubs", "diamonds" })
        {
            suitIconLabel.RemoveFromClassList(s);
            handValueLabel.RemoveFromClassList(s);
        }
        suitIconLabel.AddToClassList(suitClass);
        handValueLabel.AddToClassList(suitClass);

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        bool isMyTurn = roundManager.isPlayersTurn(playerHand.Id);
        bool isGameManagerValid = gameManager != null && gameManager.Object != null && gameManager.Object.IsValid;

        if (isGameManagerValid && gameManager.currentState == GameState.GameOver)
        {
            if (gameManager.matchWinnerHandId == playerHand.Id)
            {
                notification.Show("MATCH WINNER! YOU WIN!");
            }
            else
            {
                PlayerHand winnerHandObj = null;
                playerHand.Runner.TryFindBehaviour(gameManager.matchWinnerHandId, out winnerHandObj);
                string winnerName = winnerHandObj != null ? winnerHandObj.GetComponent<PlayerStatus>()?.playerName.Value : "";
                if (string.IsNullOrEmpty(winnerName)) winnerName = "WINNER";
                notification.Show($"GAME OVER - {winnerName.ToUpper()} WINS!");
            }
        }
        else if (phase == RoundPhase.Shooting)
        {
            if (roundManager.currentWinnerHandId == playerHand.Id)
            {
                PlayerHand target = roundManager.GetLookTarget(playerHand);
                if (target != null)
                {
                    string targetName = target.GetComponent<PlayerStatus>()?.playerName.Value;
                    if (string.IsNullOrEmpty(targetName)) targetName = "PLAYER";
                    notification.Show($"PRESS SPACE TO SHOOT {targetName.ToUpper()}!");
                }
                else
                {
                    notification.Show("LOOK AT A PLAYER TO TARGET THEM!");
                }
            }
            else
            {
                PlayerHand winnerHandObj = null;
                playerHand.Runner.TryFindBehaviour(roundManager.currentWinnerHandId, out winnerHandObj);
                string winnerName = winnerHandObj != null ? winnerHandObj.GetComponent<PlayerStatus>()?.playerName.Value : "";
                if (string.IsNullOrEmpty(winnerName)) winnerName = "WINNER";
                notification.Show($"{winnerName.ToUpper()} IS SHOOTING...");
            }
        }
        else if (phase == RoundPhase.Cooldown)
        {
            if (roundManager.shotTargetHandId == default)
            {
                notification.Show("TIE - NO ONE WAS SHOT");
            }
            else
            {
                PlayerHand target = null;
                playerHand.Runner.TryFindBehaviour(roundManager.shotTargetHandId, out target);
                string targetName = target != null ? target.GetComponent<PlayerStatus>()?.playerName.Value : "";
                if (string.IsNullOrEmpty(targetName)) targetName = "PLAYER";
                notification.Show($"{targetName.ToUpper()} WAS SHOT!");
            }
        }
        else
        {
            if (isMyTurn) notification.Show("YOUR TURN");
            else notification.Hide();
        }
    }

    void UpdateOthers()
    {
        if (othersPanel == null) return;
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

        foreach (var hand in PlayerHand.ActiveHands)
        {
            if (hand == null || hand.Object == null || !hand.Object.IsValid) continue;
            if (hand.Object.HasInputAuthority) continue;

            OtherEntry entry = GetOrCreateOther(hand);

            entry.name.text = ResolveOtherName(hand);

            PlayerStatus status = hand.GetComponent<PlayerStatus>();
            if (status != null)
                entry.health.SetValue((float)status.lives / PlayerStatus.MaxLives);

            bool gmValid = gameManager != null && gameManager.Object != null && gameManager.Object.IsValid;
            bool isTurn = gmValid && gameManager.isPlayersTurn(hand.Id);
            bool isWinner = gmValid && gameManager.phase == RoundPhase.Shooting && gameManager.currentWinnerHandId == hand.Id;
            entry.container.EnableInClassList("other-tag--turn", isTurn);
            entry.container.EnableInClassList("other-tag--winner", isWinner);
        }

        othersStale.Clear();
        foreach (var kv in others)
        {
            var hand = kv.Key;
            if (hand == null || hand.Object == null || !hand.Object.IsValid || hand.Object.HasInputAuthority)
            {
                kv.Value.container.RemoveFromHierarchy();
                othersStale.Add(hand);
            }
        }
        foreach (var h in othersStale) others.Remove(h);
    }

    OtherEntry GetOrCreateOther(PlayerHand hand)
    {
        if (others.TryGetValue(hand, out var e)) return e;

        var container = new VisualElement();
        container.AddToClassList("other-tag");
        container.pickingMode = PickingMode.Ignore;

        var name = new Label();
        name.AddToClassList("other_name");
        name.pickingMode = PickingMode.Ignore;
        container.Add(name);

        var health = new LifeBarElement();
        health.AddToClassList("other_health");
        health.SetValue(1f);
        container.Add(health);

        othersPanel.Add(container);
        e = new OtherEntry { container = container, name = name, health = health };
        others[hand] = e;
        return e;
    }

    string ResolveOtherName(PlayerHand hand)
    {
        PlayerStatus status = hand.GetComponent<PlayerStatus>();
        string n = status != null ? status.playerName.Value : null;
        if (!string.IsNullOrEmpty(n)) return n.ToUpper();

        if (hand.Object != null && hand.Object.IsValid)
            return hand.Object.InputAuthority == PlayerRef.None ? "BOT" : $"PLAYER {hand.Object.InputAuthority.PlayerId}";

        return "";
    }

    void RefreshButtons()
    {
        if (roundManager == null) return;
        bool isMyTurn = roundManager.isPlayersTurn(playerHand.Id);
        stealButton?.SetInteractable(isMyTurn);
        knockButton?.SetInteractable(isMyTurn);
        skipButton?.SetInteractable(isMyTurn);
    }

    void RefreshLifeBars()
    {
        if (playerStatus == null && playerHand != null)
            playerStatus = playerHand.GetComponent<PlayerStatus>();

        lifeBars[0].style.display = playerStatus != null ? DisplayStyle.Flex : DisplayStyle.None;
        lifeBars[1].style.display = DisplayStyle.None;
        lifeBars[2].style.display = DisplayStyle.None;
        if (playerStatus == null) return;

        if (nameLabel != null)
        {
            string n = playerStatus.playerName.Value;
            nameLabel.text = string.IsNullOrEmpty(n) ? "" : n.ToUpper();
        }

        lifeBars[0].SetValue((float)playerStatus.lives / PlayerStatus.MaxLives);
        bool isMyTurn = roundManager != null && roundManager.isPlayersTurn(playerHand.Id);
        lifeBars[0].EnableInClassList("life-bar--turn", isMyTurn);
    }



    static readonly string[] SuitSymbols = { "♠", "♥", "♣", "♦" };
    static readonly string[] SuitClasses = { "spades", "hearts", "clubs", "diamonds" };

    (string val, string symbol, string suitClass) ComputeHand()
    {
        CardData c0 = playerHand.myCards[0], c1 = playerHand.myCards[1], c2 = playerHand.myCards[2];

        if (c0.number == 0 && c1.number == 0 && c2.number == 0)
            return ("0", "♠", "spades");

        // Format three of a kind as "30½" for display, matching the 30.5 float value in the scoring rules.
        if (c0.number > 0 && c0.number == c1.number && c1.number == c2.number)
            return ("30½", SuitSymbols[(int)c0.color], SuitClasses[(int)c0.color]);

        int[] totals = new int[4];
        for (int i = 0; i < 3; i++)
            if (playerHand.myCards[i].number > 0)
                totals[(int)playerHand.myCards[i].color] += playerHand.myCards[i].gameValue;

        int best = 0, bestIdx = 0;
        for (int i = 0; i < 4; i++) if (totals[i] > best) { best = totals[i]; bestIdx = i; }
        return (best.ToString(), SuitSymbols[bestIdx], SuitClasses[bestIdx]);
    }
}
