using Fusion;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHandUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    private UIDocument doc;
    private PlayerHand playerHand;
    private GameManager gameManager;

    private readonly CardElement[] handCards = new CardElement[3];
    private GameButtonElement skipButton;
    private GameButtonElement knockButton;
    private NotificationElement notification;
    private Label dealRoundLabel;
    private Label suitIconLabel;
    private Label handValueLabel;
    private readonly LifeBarElement[] lifeBars = new LifeBarElement[3];

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
    }

    void BuildUI()
    {
        var root = doc.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        var phRoot = new VisualElement();
        phRoot.AddToClassList("ph-root");
        phRoot.pickingMode = PickingMode.Ignore;

        var top = new VisualElement();
        top.AddToClassList("ph-top");
        top.pickingMode = PickingMode.Ignore;

        notification = new NotificationElement();
        top.Add(notification);

        dealRoundLabel = new Label();
        dealRoundLabel.AddToClassList("ph-deal-label");
        dealRoundLabel.pickingMode = PickingMode.Ignore;
        top.Add(dealRoundLabel);


        var handValueRow = new VisualElement();
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

        var bottom = new VisualElement();
        bottom.AddToClassList("ph-bottom");
        bottom.pickingMode = PickingMode.Ignore;

        var cardsRow = new VisualElement();
        cardsRow.AddToClassList("ph-cards-row");
        cardsRow.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < 3; i++)
        {
            handCards[i] = new CardElement();
            handCards[i].SetEmpty();
            int idx = i;
            handCards[i].RegisterCallback<ClickEvent>(_ => playerHand?.selectMyCard(idx));
            cardsRow.Add(handCards[i]);
        }
        bottom.Add(cardsRow);

        var actions = new VisualElement();
        actions.AddToClassList("ph-actions");
        actions.pickingMode = PickingMode.Ignore;

        skipButton = new GameButtonElement();
        skipButton.Setup("SKIP >>>", GameButtonStyle.Primary, () => playerHand?.skipTurn());
        actions.Add(skipButton);

        knockButton = new GameButtonElement();
        knockButton.Setup(">> KNOCK <<", GameButtonStyle.Danger, () => playerHand?.knockTurn());
        actions.Add(knockButton);

        bottom.Add(actions);
        phRoot.Add(bottom);

        var lifeBarsRow = new VisualElement();
        lifeBarsRow.AddToClassList("ph-life-bars");
        lifeBarsRow.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < 3; i++)
        {
            lifeBars[i] = new LifeBarElement();
            lifeBars[i].SetValue(1f);
            lifeBarsRow.Add(lifeBars[i]);
        }
        phRoot.Add(lifeBarsRow);

        root.Add(phRoot);

        root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);
        foreach (var c in handCards) c.pickingMode = PickingMode.Position;
        skipButton.pickingMode = PickingMode.Position;
        knockButton.pickingMode = PickingMode.Position;
    }

    void Update()
    {
        if (playerHand == null || playerHand.Object == null || !playerHand.Object.IsValid) return;

        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

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
        if (gameManager == null) return;

        GamePhase phase = gameManager.phase;
        if (phase == GamePhase.Playing || phase == GamePhase.LastRound)
        {
            string roundStr = phase == GamePhase.LastRound ? "LAST ROUND" : $"TURN {gameManager.roundCount}";
            dealRoundLabel.text = $"DEAL {gameManager.dealCount}\n{roundStr}";
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

        bool isMyTurn = gameManager.isPlayersTurn(playerHand.Object.StateAuthority);
        if (phase == GamePhase.DealOver)
        {
            PlayerRef me = playerHand.Object.StateAuthority;
            if (gameManager.winner == PlayerRef.None) notification.Show("TIE");
            else if (gameManager.winner == me) notification.Show("YOU WIN");
            else notification.Show("YOU LOSE");
        }
        else if (isMyTurn) notification.Show("YOUR TURN");
        else notification.Hide();
    }

    void RefreshButtons()
    {
        if (gameManager == null) return;
        bool isMyTurn = gameManager.isPlayersTurn(playerHand.Object.StateAuthority);
        skipButton?.SetInteractable(isMyTurn);
        knockButton?.SetInteractable(isMyTurn);
    }

    void RefreshLifeBars()
    {
        PlayerStatus[] statuses = FindObjectsByType<PlayerStatus>();
        PlayerStatus mine = null;
        foreach (var s in statuses)
            if (s.Object.StateAuthority == playerHand.Object.StateAuthority) { mine = s; break; }

        lifeBars[0].style.display = mine != null ? DisplayStyle.Flex : DisplayStyle.None;
        lifeBars[1].style.display = DisplayStyle.None;
        lifeBars[2].style.display = DisplayStyle.None;
        if (mine == null) return;

        lifeBars[0].SetValue((float)mine.lives / PlayerStatus.MaxLives);
        bool isMyTurn = gameManager != null && gameManager.isPlayersTurn(playerHand.Object.StateAuthority);
        lifeBars[0].EnableInClassList("life-bar--turn", isMyTurn);
    }

    static readonly string[] SuitSymbols = { "♠", "♥", "♣", "♦" };
    static readonly string[] SuitClasses = { "spades", "hearts", "clubs", "diamonds" };

    (string val, string symbol, string suitClass) ComputeHand()
    {
        CardData c0 = playerHand.myCards[0], c1 = playerHand.myCards[1], c2 = playerHand.myCards[2];

        if (c0.number == 0 && c1.number == 0 && c2.number == 0)
            return ("0", "♠", "spades");

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
