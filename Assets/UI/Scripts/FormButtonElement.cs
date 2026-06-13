using System;
using UnityEngine.UIElements;

public enum FormButtonStyle { Primary, Neutral, Danger }

[UxmlElement]
public partial class FormButtonElement : VisualElement
{
    private readonly Label label;
    private Action clickAction;

    public FormButtonElement()
    {
        AddToClassList("form-button");
        label = new Label();
        label.AddToClassList("form-button_label");
        Add(label);

        RegisterCallback<ClickEvent>(_ => clickAction?.Invoke());
    }

    public void Setup(string text, Action onClick, FormButtonStyle style = FormButtonStyle.Primary)
    {
        label.text = text;
        clickAction = onClick;

        RemoveFromClassList("form-button--primary");
        RemoveFromClassList("form-button--neutral");
        RemoveFromClassList("form-button--danger");
        string styleClass = style switch
        {
            FormButtonStyle.Neutral => "form-button--neutral",
            FormButtonStyle.Danger  => "form-button--danger",
            _                       => "form-button--primary"
        };
        AddToClassList(styleClass);
    }

    public void SetInteractable(bool interactable)
    {
        SetEnabled(interactable);
        EnableInClassList("form-button--disabled", !interactable);
    }
}
