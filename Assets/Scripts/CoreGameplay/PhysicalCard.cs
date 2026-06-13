using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

[RequireComponent(typeof(UIDocument))]
public class PhysicalCard : MonoBehaviour
{
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private Transform visualPivot;
    [SerializeField] private float hoverHeight = 0.01f;
    [SerializeField] private float selectHeight = 0.02f;
    [SerializeField] private float animationDuration = 0.15f;

    public int CardIndex { get; set; }
    public bool IsTableCard { get; set; }
    public bool IsInteractable { get; set; } = true;
    public Transform VisualPivot => visualPivot;

    private UIDocument uiDocument;
    private CardElement cardElement;
    private bool isHovered;
    private bool isSelected;
    private Vector3 baseLocalPos;
    private Tween heightTween;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (visualPivot == null)
        {
            visualPivot = transform;
        }
        baseLocalPos = visualPivot.localPosition;
    }

    private void OnEnable()
    {
        isSelected = false;
        isHovered = false;
        if (visualPivot != null)
        {
            visualPivot.localPosition = baseLocalPos;
        }
        BuildUI();
    }

    private void BuildUI()
    {
        var root = uiDocument.rootVisualElement;
        root.Clear();
        root.pickingMode = PickingMode.Ignore;

        if (styleSheets != null)
        {
            foreach (var ss in styleSheets)
            {
                if (ss != null)
                {
                    root.styleSheets.Add(ss);
                }
            }
        }

        cardElement = new CardElement();
        cardElement.SetEmpty();
        root.Add(cardElement);
    }

    public void SetCardData(CardData card)
    {
        if (card.number > 0)
        {
            cardElement.SetCard(card);
            cardElement.style.opacity = 1f;
        }
        else
        {
            cardElement.SetEmpty();
            cardElement.style.opacity = 0.25f;
        }
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;
        isHovered = hovered;
        UpdateVisualHeight();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;
        cardElement.SetSelected(selected);
        UpdateVisualHeight();
    }

    public void SetTurnHighlight(bool on)
    {
        cardElement?.SetTurn(on);
    }

    private void UpdateVisualHeight()
    {
        float targetY = baseLocalPos.y;
        if (isSelected)
        {
            targetY += selectHeight;
        }
        else if (isHovered)
        {
            targetY += hoverHeight;
        }

        Vector3 targetPos = new Vector3(baseLocalPos.x, targetY, baseLocalPos.z);
        heightTween.Stop();
        heightTween = Tween.LocalPosition(visualPivot, targetPos, animationDuration, Ease.OutQuad);
    }

    private void OnDestroy()
    {
        heightTween.Stop();
    }
}
