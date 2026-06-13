using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action PlayClicked;
    public event Action SettingsClicked;
    public event Action StatsClicked;
    public event Action LeaderboardClicked;
    public event Action AccountClicked;

    private UIDocument doc;

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        BuildUI();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    void BuildUI()
    {
        var root = doc.rootVisualElement;
        root.Clear();
        if (styleSheets != null)
            foreach (var ss in styleSheets)
                if (ss != null) root.styleSheets.Add(ss);

        var menu = new VisualElement();
        menu.AddToClassList("main-menu");

        var accountBox = new VisualElement();
        accountBox.AddToClassList("main-menu_account-box");

        var account = new MenuButtonElement();
        account.AddToClassList("menu-item--account");
        account.Setup("ACCOUNT", () => AccountClicked?.Invoke());
        accountBox.Add(account);

        var pseudo = new Label();
        pseudo.AddToClassList("main-menu_pseudo");
        if (Session.IsLoggedIn) pseudo.text = Session.Username;
        else pseudo.style.display = DisplayStyle.None;
        accountBox.Add(pseudo);

        menu.Add(accountBox);

        var center = new VisualElement();
        center.AddToClassList("main-menu_center");

        var logo = new VisualElement();
        logo.AddToClassList("main-menu_logo");
        logo.name = "LogoSlot";
        center.Add(logo);

        var items = new VisualElement();
        items.AddToClassList("main-menu_items");

        var play = new MenuButtonElement();
        play.Setup("PLAY", () => PlayClicked?.Invoke());
        play.SetInteractable(Session.IsLoggedIn);
        items.Add(play);

        var settings = new MenuButtonElement();
        settings.Setup("SETTINGS", () => SettingsClicked?.Invoke());
        settings.SetInteractable(Session.IsLoggedIn);
        items.Add(settings);

        var stats = new MenuButtonElement();
        stats.Setup("MY STATS", () => StatsClicked?.Invoke());
        stats.SetInteractable(Session.IsLoggedIn);
        items.Add(stats);

        var leaderboard = new MenuButtonElement();
        leaderboard.Setup("LEADERBOARD", () => LeaderboardClicked?.Invoke());
        items.Add(leaderboard);

        center.Add(items);
        menu.Add(center);

        root.Add(menu);
    }
}
