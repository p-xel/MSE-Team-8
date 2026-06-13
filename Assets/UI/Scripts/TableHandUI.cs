using UnityEngine;
using UnityEngine.UIElements;
using Fusion;
using CoreGameplay;

public class TableHandUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    private UIDocument doc;
    private TableHand tableHand;
    private VisualElement thRoot;



    void Awake()
    {
        doc = GetComponent<UIDocument>();
        BuildUI();
    }

    void Start()
    {
        tableHand = FindAnyObjectByType<TableHand>();
        if (doc != null) doc.enabled = true;
    }

    void BuildUI()
    {
        var root = doc.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        thRoot = new VisualElement();
        thRoot.AddToClassList("th-root");
        thRoot.pickingMode = PickingMode.Ignore;
        thRoot.style.display = DisplayStyle.None;



        root.Add(thRoot);

        root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);

    }

    public void SetVisible(bool visible)
    {
        if (thRoot != null)
        {
            thRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    void Update()
    {
        var localHand = PlayerHand.localHand;
        bool isLocalPlayerSpawned = localHand != null && localHand.Object != null && localHand.Object.IsValid;

        var roundManager = FindAnyObjectByType<GameManager>();
        bool isShootingOrCooldown = roundManager != null && 
                                    roundManager.Object != null && 
                                    roundManager.Object.IsValid && 
                                    (roundManager.phase == RoundPhase.Shooting || 
                                     roundManager.phase == RoundPhase.Cooldown);

        // Hide the table hand UI elements during shooting or cooldown phases to clean up the screen.
        if (thRoot != null)
        {
            var targetDisplay = (isLocalPlayerSpawned && !isShootingOrCooldown) ? DisplayStyle.Flex : DisplayStyle.None;
            if (thRoot.style.display != targetDisplay)
                thRoot.style.display = targetDisplay;
        }

        if (!isLocalPlayerSpawned) return;

        if (tableHand == null)
            tableHand = FindAnyObjectByType<TableHand>();

        if (tableHand == null || tableHand.Object == null || !tableHand.Object.IsValid) return;


    }
}
