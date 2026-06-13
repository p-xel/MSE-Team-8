using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MyStatsUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;
    public event Action MatchesClicked;

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

        var title = new Label("MY STATS");
        title.AddToClassList("screen_title");
        screen.Add(title);

        var pseudo = new Label(Session.Username);
        pseudo.AddToClassList("screen_pseudo");
        screen.Add(pseudo);

        table = new TableElement();
        table.AddToClassList("table--narrow");
        table.SetColumns(new[] { "STAT", "VALUE" }, new[] { 2f, 1.4f });
        screen.Add(table);

        var matches = new FormButtonElement();
        matches.Setup("VIEW MY MATCHES", () => MatchesClicked?.Invoke(), FormButtonStyle.Neutral);
        screen.Add(matches);

        root.Add(screen);

        if (!string.IsNullOrEmpty(Session.Token) && ApiClient.Instance != null)
            ApiClient.Instance.GetStats(Populate, _ => { });
    }

    void Populate(AccountStatsResponse s)
    {
        if (s == null) return;
        table.ClearRows();
        table.AddRow("KILLS", s.kills.ToString("N0"));
        table.AddRow("DEFLECTS", s.deflects.ToString("N0"));
        table.AddRow("ROUNDS PLAYED", s.roundsPlayed.ToString("N0"));
        table.AddRow("GAMES PLAYED", s.gamesPlayed.ToString("N0"));
        table.AddRow("PLAYTIME", StatFormat.Time(s.totalPlaytimeSeconds));
        table.AddRow("LAST UPDATED", StatFormat.Date(s.updatedAt));
    }
}
