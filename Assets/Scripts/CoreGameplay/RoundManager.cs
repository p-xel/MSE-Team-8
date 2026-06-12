using UnityEngine;

namespace CoreGameplay
{
    public enum RoundPhase { Inactive, Playing, LastRound, Shooting, Cooldown }

    [System.Obsolete("RoundManager has been merged into GameManager. Please remove this component from your GameObjects.")]
    public class RoundManager : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning($"[Obsolete] RoundManager component is obsolete and has been merged into GameManager. Please remove this component from the GameObject: '{gameObject.name}'");
        }
    }
}
