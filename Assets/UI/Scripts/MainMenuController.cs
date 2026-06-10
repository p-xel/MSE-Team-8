using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] MainMenuUI menu;
    [SerializeField] LoginUI login;
    [SerializeField] AccountUI account;

    void Awake()
    {
        menu.AccountClicked += OnAccount;
        login.BackClicked += ShowMenu;
        login.LoginSubmitted += OnAuthenticated;
        login.RegisterSubmitted += OnAuthenticated;
        account.BackClicked += ShowMenu;
        account.LogoutClicked += OnLogout;
    }

    void Start()
    {
        ShowMenu();
    }

    void OnAccount()
    {
        if (Session.IsLoggedIn) ShowAccount();
        else ShowLogin();
    }

    void OnAuthenticated(string username)
    {
        Session.IsLoggedIn = true;
        Session.Username = username;
        ShowAccount();
    }

    void OnLogout()
    {
        Session.IsLoggedIn = false;
        Session.Username = null;
        ShowMenu();
    }

    void ShowMenu()
    {
        menu.SetVisible(true);
        login.SetVisible(false);
        account.SetVisible(false);
    }

    void ShowLogin()
    {
        menu.SetVisible(false);
        login.SetVisible(true);
        account.SetVisible(false);
    }

    void ShowAccount()
    {
        menu.SetVisible(false);
        login.SetVisible(false);
        account.SetVisible(true);
    }
}
