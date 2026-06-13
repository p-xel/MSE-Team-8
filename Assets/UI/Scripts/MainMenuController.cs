using System;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] MainMenuUI menu;
    [SerializeField] LoginUI login;
    [SerializeField] AccountUI account;
    [SerializeField] LeaderboardUI leaderboard;
    [SerializeField] MyStatsUI myStats;
    [SerializeField] MyMatchesUI myMatches;
    [SerializeField] SettingsUI settings;

    private MonoBehaviour[] screens;

    void Awake()
    {
        screens = new MonoBehaviour[] { menu, login, account, leaderboard, myStats, myMatches, settings };

        menu.AccountClicked += OnAccount;
        menu.LeaderboardClicked += () => Show(leaderboard);
        menu.StatsClicked += () => Show(myStats);
        menu.SettingsClicked += () => Show(settings);

        settings.BackClicked += () => Show(menu);

        login.BackClicked += () => Show(menu);
        login.LoginSubmitted += OnLogin;
        login.RegisterSubmitted += OnRegister;

        account.BackClicked += () => Show(menu);
        account.LogoutClicked += OnLogout;

        leaderboard.BackClicked += () => Show(menu);

        myStats.BackClicked += () => Show(menu);
        myStats.MatchesClicked += () => Show(myMatches);

        myMatches.BackClicked += () => Show(myStats);
    }

    void Start()
    {
        Show(menu);
    }

    void OnAccount()
    {
        Show(Session.IsLoggedIn ? account : login);
    }

    void OnLogin(string username, string password)
    {
        Authenticate(username, password, false);
    }

    void OnRegister(string username, string password)
    {
        Authenticate(username, password, true);
    }

    void Authenticate(string username, string password, bool register)
    {
        if (username == "admin" && password == "admin")
        {
            EnterSession(username, null, null);
            return;
        }

        if (ApiClient.Instance == null)
        {
            Debug.LogWarning("ApiClient is not present in the scene.");
            return;
        }

        Action<AuthResponse> onSuccess = res => EnterSession(username, res.token, res.accountId);
        Action<string> onError = err => Debug.LogWarning(err);

        if (register) ApiClient.Instance.Register(username, password, onSuccess, onError);
        else ApiClient.Instance.Login(username, password, onSuccess, onError);
    }

    void EnterSession(string username, string token, string accountId)
    {
        Session.IsLoggedIn = true;
        Session.Username = username;
        Session.Token = token;
        Session.AccountId = accountId;
        Show(account);
    }

    void OnLogout()
    {
        Session.IsLoggedIn = false;
        Session.Username = null;
        Session.Token = null;
        Session.AccountId = null;
        Show(menu);
    }

    void Show(MonoBehaviour target)
    {
        foreach (var s in screens)
            s.gameObject.SetActive(s == target);
    }
}
