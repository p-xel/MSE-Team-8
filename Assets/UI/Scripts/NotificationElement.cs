using UnityEngine.UIElements;

[UxmlElement]
public partial class NotificationElement : VisualElement
{
    private readonly Label label;

    public NotificationElement()
    {
        AddToClassList("notification");
        AddToClassList("notification--hidden");
        pickingMode = PickingMode.Ignore;
        label = new Label();
        label.AddToClassList("notification_label");
        label.pickingMode = PickingMode.Ignore;
        Add(label);
    }


    public void Show(string text)
    {
        label.text = text;
        RemoveFromClassList("notification--hidden");
        AddToClassList("notification--visible");
    }

    public void Hide()
    {
        RemoveFromClassList("notification--visible");
        AddToClassList("notification--hidden");
    }

    public void ShowTemporary(string text, float durationSeconds)
    {
        Show(text);
        schedule.Execute(Hide).StartingIn((long)(durationSeconds * 1000));
    }
}
