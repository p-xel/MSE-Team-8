using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;

    private UIDocument doc;
    private TableElement table;

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        BuildUI();
    }

    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    void BuildUI()
    {
        var root = doc.rootVisualElement;
        root.Clear();
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        var screen = new VisualElement();
        screen.AddToClassList("screen");

        var back = new MenuButtonElement();
        back.AddToClassList("menu-item--account");
        back.AddToClassList("screen_back");
        back.Setup("< BACK", () => BackClicked?.Invoke());
        screen.Add(back);

        var title = new Label("LEADERBOARD");
        title.AddToClassList("screen_title");
        screen.Add(title);

        table = new TableElement();
        table.SetColumns(
            new[] { "#", "PLAYER", "LEVEL", "EXP" },
            new[] { 0.6f, 2.4f, 1f, 1.4f });

        screen.Add(table);
        root.Add(screen);

        if (ApiClient.Instance != null)
            ApiClient.Instance.GetLeaderboard(Populate, _ => { });
    }

    void Populate(List<LeaderboardEntryResponse> entries)
    {
        if (entries == null) return;
        table.ClearRows();
        foreach (var e in entries)
            table.AddRow(e.rank.ToString(), e.username, e.level.ToString(), e.exp.ToString("N0"));
    }
}
