using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RoomFinderUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;

    private const float RoomWeight = 2.2f;
    private const float PlayersWeight = 3f;
    private const float JoinWeight = 1f;

    private UIDocument doc;
    private ScrollView list;
    private Label status;

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        BuildUI();

        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.SessionsUpdated += OnSessions;
            NetworkLauncher.Instance.JoinLobby();
            Populate(new List<SessionInfo>(NetworkLauncher.Instance.Sessions));
        }
        else
        {
            status.text = "Network launcher unavailable.";
        }
    }

    void OnDisable()
    {
        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.SessionsUpdated -= OnSessions;
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

        var title = new Label("ROOM FINDER");
        title.AddToClassList("screen_title");
        title.style.marginBottom = 24;
        screen.Add(title);

        var actions = new VisualElement();
        actions.AddToClassList("room-finder_actions");
        actions.style.marginBottom = 24;

        var create = new FormButtonElement();
        create.Setup("CREATE ROOM", () => NetworkLauncher.Instance?.CreateRoom());
        create.style.marginBottom = 12;
        actions.Add(create);

        var refresh = new FormButtonElement();
        refresh.Setup("REFRESH", () => NetworkLauncher.Instance?.JoinLobby(), FormButtonStyle.Neutral);
        actions.Add(refresh);

        screen.Add(actions);

        var table = new VisualElement();
        table.AddToClassList("table");

        var header = new VisualElement();
        header.AddToClassList("table_header");
        header.Add(HeaderCell("ROOM", RoomWeight));
        header.Add(HeaderCell("PLAYERS", PlayersWeight));
        header.Add(HeaderCell("", JoinWeight));
        table.Add(header);

        list = new ScrollView(ScrollViewMode.Vertical);
        list.AddToClassList("table_body");
        list.AddToClassList("room-finder_list");
        list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        table.Add(list);

        table.style.marginBottom = 24;
        screen.Add(table);

        status = new Label("Searching for rooms...");
        status.AddToClassList("room-finder_status");
        screen.Add(status);

        root.Add(screen);
    }

    void OnSessions(List<SessionInfo> sessions) => Populate(sessions);

    void Populate(List<SessionInfo> sessions)
    {
        if (list == null) return;
        list.Clear();

        int shown = 0;
        if (sessions != null)
        {
            foreach (var info in sessions)
            {
                if (info == null || !info.IsValid || !info.IsVisible) continue;
                AddRow(info);
                shown++;
            }
        }

        status.text = shown == 0 ? "" : $"{shown} room(s) found.";
    }

    void AddRow(SessionInfo info)
    {
        bool full = info.PlayerCount >= info.MaxPlayers;

        var row = new VisualElement();
        row.AddToClassList("table_row");
        if (list.childCount % 2 == 1) row.AddToClassList("table_row--alt");

        row.Add(Cell(info.Name, RoomWeight));
        row.Add(Cell(PlayersText(info), PlayersWeight));

        var join = new FormButtonElement();
        string sessionName = info.Name;
        join.Setup(full ? "FULL" : "JOIN", () => NetworkLauncher.Instance?.JoinRoom(sessionName));
        join.SetInteractable(!full && info.IsOpen);
        join.style.flexGrow = JoinWeight;
        join.style.flexBasis = new StyleLength(0f);
        row.Add(join);

        list.Add(row);
    }

    string PlayersText(SessionInfo info)
    {
        var names = new List<string>();
        if (info.Properties != null)
        {
            foreach (var kv in info.Properties)
            {
                if (kv.Key.StartsWith(NetworkLauncher.PlayerNameKeyPrefix) && kv.Value.IsString)
                {
                    string playerName = kv.Value;
                    if (!string.IsNullOrEmpty(playerName)) names.Add(playerName);
                }
            }
        }

        string count = $"{info.PlayerCount}/{info.MaxPlayers}";
        return names.Count > 0 ? $"{count}  {string.Join(", ", names)}" : count;
    }

    Label HeaderCell(string text, float weight)
    {
        var cell = new Label(text);
        cell.AddToClassList("table_header-cell");
        cell.style.flexGrow = weight;
        cell.style.flexBasis = new StyleLength(0f);
        return cell;
    }

    Label Cell(string text, float weight)
    {
        var cell = new Label(text);
        cell.AddToClassList("table_cell");
        cell.style.flexGrow = weight;
        cell.style.flexBasis = new StyleLength(0f);
        return cell;
    }
}
