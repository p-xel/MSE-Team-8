using Fusion;
using UnityEngine;

// enum for the 4 suits (colors) of the cards
public enum CardColor
{
    Spades,
    Hearts,
    Clubs,
    Diamonds
}

// network struct to hold card data across clients
[System.Serializable]
public struct CardData : INetworkStruct
{
    public CardColor color;
    public int number;
    public int gameValue;

    // helper method to format text for ui display
    public override string ToString()
    {
        string numStr = number.ToString();
        switch (number)
        {
            case 1: numStr = "A"; break;
            case 11: numStr = "J"; break;
            case 12: numStr = "Q"; break;
            case 13: numStr = "K"; break;
        }
        return $"{color} {numStr} (val: {gameValue})";
    }
}
