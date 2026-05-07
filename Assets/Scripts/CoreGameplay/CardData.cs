using Fusion;
using UnityEngine;

public enum CardColor
{
    Spades,
    Hearts,
    Clubs,
    Diamonds
}

[System.Serializable]
public struct CardData : INetworkStruct
{
    public CardColor color;
    public int number;
    public int gameValue;

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
