using Fusion;

public class PlayerStatus : NetworkBehaviour
{
    public const int MaxLives = 3;

    [Networked] public int lives { get; set; }
    [Networked] public int dealPoints { get; set; }
    [Networked] public int kills { get; set; }
    [Networked] public int deflects { get; set; }
    [Networked] public NetworkString<_32> playerName { get; set; }
    [Networked] public int roundsWon { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            lives = MaxLives;
            if (Object.InputAuthority == PlayerRef.None)
                playerName = $"Bot {Object.Id.Raw % 100}";
            else
                playerName = string.IsNullOrEmpty(Session.Username) ? $"Player {Object.InputAuthority.PlayerId}" : Session.Username;
        }
    }
}
