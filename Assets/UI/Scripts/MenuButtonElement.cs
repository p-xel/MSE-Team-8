using System;
using UnityEngine.UIElements;

[UxmlElement]
public partial class MenuButtonElement : VisualElement
{
    private readonly Label text;
    private readonly Label glitchCyan;
    private readonly Label glitchMagenta;
    private Action clickAction;

    public MenuButtonElement()
    {
        AddToClassList("menu-item");

        var leftChevron = new Label(">>");
        leftChevron.AddToClassList("menu-item_chevron");
        leftChevron.AddToClassList("menu-item_chevron--left");
        Add(leftChevron);

        var wrap = new VisualElement();
        wrap.AddToClassList("menu-item_text-wrap");

        glitchMagenta = new Label();
        glitchMagenta.AddToClassList("menu-item_text");
        glitchMagenta.AddToClassList("menu-item_text--glitch");
        glitchMagenta.AddToClassList("menu-item_text--magenta");
        wrap.Add(glitchMagenta);

        glitchCyan = new Label();
        glitchCyan.AddToClassList("menu-item_text");
        glitchCyan.AddToClassList("menu-item_text--glitch");
        glitchCyan.AddToClassList("menu-item_text--cyan");
        wrap.Add(glitchCyan);

        text = new Label();
        text.AddToClassList("menu-item_text");
        wrap.Add(text);

        Add(wrap);

        var rightChevron = new Label("<<");
        rightChevron.AddToClassList("menu-item_chevron");
        rightChevron.AddToClassList("menu-item_chevron--right");
        Add(rightChevron);

        RegisterCallback<ClickEvent>(_ => clickAction?.Invoke());
    }

    public void Setup(string label, Action onClick)
    {
        text.text = label;
        glitchCyan.text = label;
        glitchMagenta.text = label;
        clickAction = onClick;
    }

    public void SetInteractable(bool interactable)
    {
        SetEnabled(interactable);
        EnableInClassList("menu-item--disabled", !interactable);
    }
}
