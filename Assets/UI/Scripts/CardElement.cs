using UnityEngine.UIElements;

[UxmlElement]
public partial class CardElement : VisualElement
{
    private static readonly string[] SuitSymbols = { "♠", "♥", "♣", "♦" };
    private static readonly string[] SuitClasses = { "spades", "hearts", "clubs", "diamonds" };

    private readonly Label topRank;
    private readonly Label topSuit;
    private readonly Label centerSuit;
    private readonly Label bottomRank;
    private readonly Label bottomSuit;

    public CardElement()
    {
        AddToClassList("card");

        var topCorner = new VisualElement();
        topCorner.AddToClassList("card_corner");
        topCorner.AddToClassList("card_corner--top");
        topRank = new Label();
        topRank.AddToClassList("card_rank");
        topSuit = new Label();
        topSuit.AddToClassList("card_suit-small");
        topCorner.Add(topRank);
        topCorner.Add(topSuit);

        centerSuit = new Label();
        centerSuit.AddToClassList("card_suit-center");

        var bottomCorner = new VisualElement();
        bottomCorner.AddToClassList("card_corner");
        bottomCorner.AddToClassList("card_corner--bottom");
        bottomRank = new Label();
        bottomRank.AddToClassList("card_rank");
        bottomSuit = new Label();
        bottomSuit.AddToClassList("card_suit-small");
        bottomCorner.Add(bottomRank);
        bottomCorner.Add(bottomSuit);

        Add(topCorner);
        Add(centerSuit);
        Add(bottomCorner);
    }

    public void SetCard(CardData card)
    {
        string rank = card.number switch
        {
            1  => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _  => card.number.ToString()
        };

        string symbol = SuitSymbols[(int)card.color];
        string suitClass = SuitClasses[(int)card.color];

        topRank.text = rank;
        bottomRank.text = rank;
        topSuit.text = symbol;
        bottomSuit.text = symbol;
        centerSuit.text = symbol;

        foreach (string s in SuitClasses)
        {
            topSuit.RemoveFromClassList(s);
            bottomSuit.RemoveFromClassList(s);
            centerSuit.RemoveFromClassList(s);
        }

        topSuit.AddToClassList(suitClass);
        bottomSuit.AddToClassList(suitClass);
        centerSuit.AddToClassList(suitClass);

        RemoveFromClassList("card--empty");
    }

    public void SetEmpty()
    {
        topRank.text = string.Empty;
        bottomRank.text = string.Empty;
        topSuit.text = string.Empty;
        bottomSuit.text = string.Empty;
        centerSuit.text = string.Empty;
        AddToClassList("card--empty");
    }

    public void SetSelected(bool selected)
    {
        if (selected) AddToClassList("card--selected");
        else RemoveFromClassList("card--selected");
    }
}
