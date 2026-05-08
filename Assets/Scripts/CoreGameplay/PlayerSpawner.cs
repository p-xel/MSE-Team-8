using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    public GameObject PlayerPrefab;

    void IPlayerJoined.PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
            Runner.Spawn(PlayerPrefab, new Vector3(0, 1, 0), Quaternion.identity, player);
    }

    void IPlayerLeft.PlayerLeft(PlayerRef player)
    {
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid && hand.Object.StateAuthority == player)
            {
                Runner.Despawn(hand.Object);
                return;
            }
        }
    }
}
