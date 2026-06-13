using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoginUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;
    public event Action<string, string> LoginSubmitted;
    public event Action<string, string> RegisterSubmitted;

    private UIDocument doc;
    private TextField usernameField;
    private TextField passwordField;

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

        var back = new MenuButtonElement();
        back.AddToClassList("menu-item--account");
        back.AddToClassList("screen_back");
        back.Setup("< BACK", () => BackClicked?.Invoke());
        screen.Add(back);

        var card = new VisualElement();
        card.AddToClassList("login-card");

        var title = new Label("LOGIN");
        title.AddToClassList("screen_title");
        title.AddToClassList("login-card_title");
        card.Add(title);

        card.Add(MakeField("USERNAME", false, out usernameField));
        card.Add(MakeField("PASSWORD", true, out passwordField));

        var login = new FormButtonElement();
        login.Setup("LOG IN", () => LoginSubmitted?.Invoke(usernameField.value, passwordField.value));
        card.Add(login);

        var register = new FormButtonElement();
        register.Setup("CREATE ACCOUNT", () => RegisterSubmitted?.Invoke(usernameField.value, passwordField.value), FormButtonStyle.Danger);
        card.Add(register);

        screen.Add(card);
        root.Add(screen);
    }

    VisualElement MakeField(string caption, bool password, out TextField field)
    {
        var group = new VisualElement();
        group.AddToClassList("screen_field-group");

        var label = new Label(caption);
        label.AddToClassList("screen_caption");
        group.Add(label);

        field = new TextField();
        field.isPasswordField = password;
        field.AddToClassList("screen_input");
        group.Add(field);

        return group;
    }
}
