using System;
using UnityEngine.UIElements;

public enum GameButtonStyle { Primary, Danger, Special }

[UxmlElement]
public partial class GameButtonElement : VisualElement
{
    private readonly Label label;
    private Action clickAction;

    public GameButtonElement()
    {
        AddToClassList("game-button");
        label = new Label();
        label.AddToClassList("game-button_label");
        Add(label);

        RegisterCallback<ClickEvent>(_ => clickAction?.Invoke());
    }

    public void Setup(string text, GameButtonStyle style, Action onClick)
    {
        label.text = text;
        clickAction = onClick;

        RemoveFromClassList("game-button--primary");
        RemoveFromClassList("game-button--danger");
        RemoveFromClassList("game-button--special");

        string styleClass = style switch
        {
            GameButtonStyle.Primary => "game-button--primary",
            GameButtonStyle.Danger  => "game-button--danger",
            GameButtonStyle.Special => "game-button--special",
            _                       => "game-button--primary"
        };
        AddToClassList(styleClass);
    }

    public void SetInteractable(bool interactable)
    {
        SetEnabled(interactable);
        if (interactable) RemoveFromClassList("game-button--disabled");
        else AddToClassList("game-button--disabled");
    }
}
