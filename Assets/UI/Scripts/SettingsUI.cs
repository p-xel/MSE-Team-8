using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SettingsUI : MonoBehaviour
{
    [SerializeField] StyleSheet[] styleSheets;

    public event Action BackClicked;

    private UIDocument doc;
    private Label skinName;

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

        var title = new Label("SETTINGS");
        title.AddToClassList("screen_title");
        title.AddToClassList("login-card_title");
        screen.Add(title);

        var card = new VisualElement();
        card.AddToClassList("login-card");

        var soundGroup = new VisualElement();
        soundGroup.AddToClassList("screen_field-group");
        var soundLabel = new Label("SOUND");
        soundLabel.AddToClassList("screen_caption");
        soundGroup.Add(soundLabel);
        var slider = new Slider(0f, 1f) { value = Session.Volume };
        slider.AddToClassList("settings_slider");
        slider.RegisterValueChangedCallback(e => Session.Volume = e.newValue);
        soundGroup.Add(slider);
        card.Add(soundGroup);

        var skinGroup = new VisualElement();
        skinGroup.AddToClassList("screen_field-group");
        var skinLabel = new Label("SKIN");
        skinLabel.AddToClassList("screen_caption");
        skinGroup.Add(skinLabel);

        var selector = new VisualElement();
        selector.AddToClassList("settings_selector");
        var prev = new Button(() => CycleSkin(-1)) { text = "<" };
        prev.AddToClassList("settings_arrow");
        skinName = new Label(Session.Skin.ToString());
        skinName.AddToClassList("settings_skin-name");
        var next = new Button(() => CycleSkin(1)) { text = ">" };
        next.AddToClassList("settings_arrow");
        selector.Add(prev);
        selector.Add(skinName);
        selector.Add(next);
        skinGroup.Add(selector);
        card.Add(skinGroup);

        screen.Add(card);
        root.Add(screen);

        if (!string.IsNullOrEmpty(Session.Token) && ApiClient.Instance != null)
            ApiClient.Instance.GetMe(LoadSkin, _ => { });
    }

    void LoadSkin(AccountResponse account)
    {
        if (account == null) return;
        if (Enum.TryParse(account.selectedCharacter, out Skin skin))
        {
            Session.Skin = skin;
            skinName.text = Session.Skin.ToString();
        }
    }

    void CycleSkin(int dir)
    {
        var values = (Skin[])Enum.GetValues(typeof(Skin));
        int i = Array.IndexOf(values, Session.Skin);
        i = (i + dir + values.Length) % values.Length;
        Session.Skin = values[i];
        skinName.text = Session.Skin.ToString();

        if (!string.IsNullOrEmpty(Session.Token) && ApiClient.Instance != null)
            ApiClient.Instance.UpdateSelectedCharacter(Session.Skin.ToString(), _ => { }, _ => { });
    }
}
