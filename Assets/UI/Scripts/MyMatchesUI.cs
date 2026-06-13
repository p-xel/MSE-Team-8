using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MyMatchesUI : MonoBehaviour
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

        var title = new Label("MY MATCHES");
        title.AddToClassList("screen_title");
        screen.Add(title);

        table = new TableElement();
        table.SetColumns(
            new[] { "MATCH", "RESULT", "KILLS", "DEFLECTS", "ROUNDS", "PLAYTIME", "DATE" },
            new[] { 1.6f, 1f, 0.9f, 1.6f, 1f, 1.2f, 1.4f });

        screen.Add(table);
        root.Add(screen);

        if (!string.IsNullOrEmpty(Session.Token) && ApiClient.Instance != null)
            ApiClient.Instance.GetMatches(Populate, _ => { });
    }

    void Populate(List<MatchRecordResponse> matches)
    {
        if (matches == null) return;
        table.ClearRows();
        foreach (var m in matches)
            table.AddRow(m.matchId, m.result, m.kills.ToString(), m.deflects.ToString(), m.roundsPlayed.ToString(), StatFormat.Time(m.playtimeSeconds), StatFormat.Date(m.playedAt));
    }
}
