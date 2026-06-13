using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AccountUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;
    public event Action LogoutClicked;

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

        var screen = new VisualElement();
        screen.AddToClassList("screen");

        var title = new Label("ACCOUNT");
        title.AddToClassList("screen_title");
        screen.Add(title);

        var pseudo = new Label(Session.Username);
        pseudo.AddToClassList("screen_pseudo");
        screen.Add(pseudo);

        var logoutButton = new FormButtonElement();
        logoutButton.Setup("LOG OUT", () => LogoutClicked?.Invoke(), FormButtonStyle.Danger);
        screen.Add(logoutButton);

        var backButton = new MenuButtonElement();
        backButton.AddToClassList("menu-item--account");
        backButton.AddToClassList("screen_back");
        backButton.Setup("< BACK", () => BackClicked?.Invoke());
        screen.Add(backButton);

        root.Add(screen);
    }
}
