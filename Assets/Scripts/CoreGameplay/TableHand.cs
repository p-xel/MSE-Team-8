using Fusion;
using UnityEngine;

public class TableHand : NetworkBehaviour
{
    [Networked, Capacity(3)]
    public NetworkArray<CardData> tableCards { get; }

    public void initialize(GameDeck deck)
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < 3; i++)
            tableCards.Set(i, deck.drawCard());
    }

    public void performSwap(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        if (tableIndex < 0 || tableIndex >= 3) return;
        if (!Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand)) return;

        CardData oldTableCard = tableCards[tableIndex];
        tableCards.Set(tableIndex, playerCard);

        if (playerHand.Object.HasStateAuthority)
            playerHand.myCards.Set(handIndex, oldTableCard);
        else
            playerHand.Rpc_ReceiveSwappedCard(oldTableCard, handIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_SwapCard(int tableIndex, CardData playerCard, NetworkBehaviourId playerHandId, int handIndex)
    {
        performSwap(tableIndex, playerCard, playerHandId, handIndex);
    }

    public void stealAll(NetworkBehaviourId playerHandId)
    {
        if (!Runner.TryFindBehaviour(playerHandId, out PlayerHand playerHand)) return;

        CardData t0 = tableCards[0], t1 = tableCards[1], t2 = tableCards[2];
        tableCards.Set(0, playerHand.myCards[0]);
        tableCards.Set(1, playerHand.myCards[1]);
        tableCards.Set(2, playerHand.myCards[2]);

        if (playerHand.Object.HasStateAuthority)
        {
            playerHand.myCards.Set(0, t0);
            playerHand.myCards.Set(1, t1);
            playerHand.myCards.Set(2, t2);
        }
        else
            playerHand.Rpc_ReceiveNewHand(t0, t1, t2);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, InvokeLocal = false)]
    public void Rpc_StealAll(NetworkBehaviourId playerHandId)
    {
        stealAll(playerHandId);
    }
}
