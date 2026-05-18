using UnityEngine;
using UnityEngine.UIElements;

public class TableHandUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    private UIDocument doc;
    private TableHand tableHand;

    private readonly CardElement[] tableCards = new CardElement[3];
    private GameButtonElement stealButton;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        BuildUI();
    }

    void Start()
    {
        tableHand = FindAnyObjectByType<TableHand>();
    }

    void BuildUI()
    {
        var root = doc.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        var thRoot = new VisualElement();
        thRoot.AddToClassList("th-root");
        thRoot.pickingMode = PickingMode.Ignore;

        var cardsRow = new VisualElement();
        cardsRow.AddToClassList("th-cards-row");
        cardsRow.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < 3; i++)
        {
            tableCards[i] = new CardElement();
            tableCards[i].SetEmpty();
            int idx = i;
            tableCards[i].RegisterCallback<ClickEvent>(_ => PlayerHand.localHand?.selectTableCard(idx));
            cardsRow.Add(tableCards[i]);
        }
        thRoot.Add(cardsRow);

        stealButton = new GameButtonElement();
        stealButton.AddToClassList("th-steal-btn");
        stealButton.Setup(">> STEAL THE TABLE <<", GameButtonStyle.Special, () => PlayerHand.localHand?.stealTable());
        stealButton.style.display = DisplayStyle.None;
        thRoot.Add(stealButton);

        root.Add(thRoot);

        root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);
        foreach (var c in tableCards) c.pickingMode = PickingMode.Position;
        stealButton.pickingMode = PickingMode.Position;
    }

    void Update()
    {
        if (tableHand == null)
        {
            tableHand = FindAnyObjectByType<TableHand>();
            return;
        }
        if (tableHand.Object == null || !tableHand.Object.IsValid) return;

        bool isMyTurn = PlayerHand.localHand != null
            && PlayerHand.localHand.Object != null
            && FindAnyObjectByType<GameManager>() is GameManager gm
            && gm.isPlayersTurn(PlayerHand.localHand.Object.StateAuthority);

        for (int i = 0; i < 3; i++)
        {
            CardData card = tableHand.tableCards[i];
            tableCards[i].style.opacity = card.number > 0 ? 1f : 0.25f;
            if (card.number > 0) tableCards[i].SetCard(card);
            else tableCards[i].SetEmpty();
        }

        stealButton.style.display = isMyTurn ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
