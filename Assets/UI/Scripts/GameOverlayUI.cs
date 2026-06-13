using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using CoreGameplay;

[RequireComponent(typeof(UIDocument))]
public class GameOverlayUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    private UIDocument _doc;
    private VisualElement _modal;
    private Label _modalTitle;
    private Label _modalSubtitle;
    private VisualElement _statsContainer;
    private FormButtonElement _actionButton;

    private GameManager _gm;
    private PlayerHand _localHand;

    private GameState _lastState = (GameState)(-1);
    private float _matchStartTime;
    private bool _submitted;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        BuildUI();
    }

    void Start()
    {
        _matchStartTime = Time.time;
    }

    void BuildUI()
    {
        var root = _doc.rootVisualElement;
        root.Clear();
        root.pickingMode = PickingMode.Ignore;
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        var container = new VisualElement();
        container.AddToClassList("go-root");
        container.pickingMode = PickingMode.Ignore;

        _modal = new VisualElement();
        _modal.AddToClassList("go-modal");
        _modal.style.display = DisplayStyle.None;

        _modalTitle = new Label();
        _modalTitle.AddToClassList("go-modal_title");
        _modal.Add(_modalTitle);

        _modalSubtitle = new Label();
        _modalSubtitle.AddToClassList("go-modal_subtitle");
        _modal.Add(_modalSubtitle);

        _statsContainer = new VisualElement();
        _statsContainer.AddToClassList("go-stats");
        _modal.Add(_statsContainer);

        _actionButton = new FormButtonElement();
        _modal.Add(_actionButton);

        container.Add(_modal);
        root.Add(container);
    }

    void Update()
    {
        if (_gm == null)
        {
            _gm = FindAnyObjectByType<GameManager>();
            if (_gm == null) return;
        }
        if (_gm.Object == null || !_gm.Object.IsValid) return;

        if (_localHand == null)
        {
            foreach (var hand in FindObjectsByType<PlayerHand>())
                if (hand.Object != null && hand.Object.IsValid && hand.Object.HasInputAuthority) { _localHand = hand; break; }
        }

        GameState state = _gm.currentState;
        if (state != _lastState)
        {
            _lastState = state;
            OnStateChanged(state);
        }
    }

    void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Lobby:
                PopulateLobbyPlayers();
                _modalSubtitle.text = $"ROOM  {RoomId()}";
                ShowModal("WAITING FOR PLAYERS", "START GAME", FormButtonStyle.Primary, () =>
                {
                    _gm?.Rpc_StartGame();
                    HideModal();
                });
                break;

            case GameState.Playing:
                _matchStartTime = Time.time;
                _submitted = false;
                HideModal();
                break;

            case GameState.GameOver:
                SubmitMatchResult();
                ShowGameOverModal();
                break;
        }
    }

    void ShowGameOverModal()
    {
        _statsContainer.Clear();

        var rows = new List<(string name, int roundsWon, long kills, int lives, bool isWinner)>();
        foreach (var hand in FindObjectsByType<PlayerHand>())
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;
            var status = hand.GetComponent<PlayerStatus>();
            if (status == null) continue;
            string name = status.playerName.Value;
            if (string.IsNullOrEmpty(name))
                name = hand.Object.InputAuthority == PlayerRef.None ? "BOT" : $"Player {hand.Object.InputAuthority.PlayerId}";
            rows.Add((name, status.roundsWon, status.kills, status.lives, hand.Id == _gm.matchWinnerHandId));
        }

        if (rows.Count > 0)
        {
            var header = new VisualElement();
            header.AddToClassList("go-stats_row");
            header.AddToClassList("go-stats_row--header");
            header.Add(StatsCell("PLAYER", true));
            header.Add(StatsCell("ROUNDS WON", true));
            header.Add(StatsCell("SHOTS FIRED", true));
            header.Add(StatsCell("LIVES LEFT", true));
            _statsContainer.Add(header);

            foreach (var r in rows)
            {
                var row = new VisualElement();
                row.AddToClassList("go-stats_row");
                if (r.isWinner) row.AddToClassList("go-stats_row--winner");
                row.Add(StatsCell(r.name.ToUpper(), false));
                row.Add(StatsCell(r.roundsWon.ToString(), false));
                row.Add(StatsCell(r.kills.ToString(), false));
                row.Add(StatsCell(r.lives.ToString(), false));
                _statsContainer.Add(row);
            }
        }

        bool localWon = _localHand != null && _localHand.Id == _gm.matchWinnerHandId;
        _modalSubtitle.text = $"ROOM  {RoomId()}";
        ShowModal(localWon ? "YOU WIN!" : "GAME OVER", "LEAVE GAME", FormButtonStyle.Danger, LeaveGame);
    }

    void PopulateLobbyPlayers()
    {
        _statsContainer.Clear();
        foreach (var hand in FindObjectsByType<PlayerHand>())
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;
            var status = hand.GetComponent<PlayerStatus>();
            string name = status != null ? status.playerName.Value : null;
            if (string.IsNullOrEmpty(name))
                name = hand.Object.InputAuthority == PlayerRef.None ? "BOT" : $"Player {hand.Object.InputAuthority.PlayerId}";

            var row = new VisualElement();
            row.AddToClassList("go-stats_row");
            row.Add(StatsCell(name.ToUpper(), false));
            _statsContainer.Add(row);
        }
    }

    string RoomId()
    {
        string id = _gm?.Runner?.SessionInfo?.Name;
        if (string.IsNullOrEmpty(id)) id = Session.MatchId;
        return string.IsNullOrEmpty(id) ? "SOLO" : id;
    }

    void ShowModal(string title, string buttonText, FormButtonStyle buttonStyle, System.Action buttonAction)
    {
        _modalTitle.text = title;
        _actionButton.Setup(buttonText, buttonAction, buttonStyle);
        _modal.style.display = DisplayStyle.Flex;
    }

    void HideModal()
    {
        _modal.style.display = DisplayStyle.None;
    }

    void SubmitMatchResult()
    {
        if (_submitted || !Session.IsLoggedIn || ApiClient.Instance == null || _gm == null || _localHand == null) return;
        _submitted = true;

        string matchId = _gm.Runner?.SessionInfo?.Name;
        if (string.IsNullOrEmpty(matchId)) matchId = Session.MatchId;
        if (string.IsNullOrEmpty(matchId)) matchId = System.Guid.NewGuid().ToString();

        var status = _localHand.GetComponent<PlayerStatus>();
        bool localWon = _localHand.Id == _gm.matchWinnerHandId;

        ApiClient.Instance.SubmitMatch(new MatchSubmissionRequest
        {
            matchId = matchId,
            kills = status != null ? status.kills : 0,
            deflects = status != null ? status.deflects : 0,
            roundsPlayed = _gm.roundCount,
            playtimeSeconds = (long)(Time.time - _matchStartTime),
            result = localWon ? "WIN" : "LOSS"
        },
        _ => Debug.Log("[GameOverlayUI] Match submitted."),
        err => Debug.LogWarning($"[GameOverlayUI] Match submit failed: {err}"));
    }

    void LeaveGame()
    {
        var runner = _gm?.Runner;
        if (runner != null && runner.IsRunning) runner.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }


    Label StatsCell(string text, bool isHeader)
    {
        var cell = new Label(text);
        cell.AddToClassList(isHeader ? "go-stats_cell--header" : "go-stats_cell");
        return cell;
    }
}
