using UnityEngine;
using System.Collections.Generic;
using PrimeTween;
using Fusion;

public class CardManager3D : MonoBehaviour
{
    [SerializeField] private GameObject physicalCardPrefab;
    [SerializeField] private float arcHeight = 0.3f;
    [SerializeField] private float flightDuration = 0.4f;
    [SerializeField] private float localPlayerYOffset = 0.35f;
    [SerializeField] private float localPlayerZOffset = 0.5f;
    [SerializeField] private float remotePlayerYOffset = 0.32f;
    [SerializeField] private float remotePlayerZOffset = 0.4f;
    [SerializeField] private float cardSpacing = 0.25f;
    [SerializeField] private float tableCardSpacing = 0.4f;

    private class HandVisuals
    {
        public PhysicalCard[] cards = new PhysicalCard[3];
        public CardData[] cachedData = new CardData[3];
        public bool[] isHiding = new bool[3];
    }

    private TableHand tableHand;
    private GameManager gameManager;
    private HandVisuals tableVisuals = new HandVisuals();
    private Dictionary<NetworkBehaviourId, HandVisuals> playerVisualsMap = new Dictionary<NetworkBehaviourId, HandVisuals>();

    private void Update()
    {
        UpdateTableCards();
        UpdatePlayerCards();
    }

    private void UpdateTableCards()
    {
        if (tableHand == null)
        {
            tableHand = FindAnyObjectByType<TableHand>();
        }

        if (tableHand == null || tableHand.Object == null || !tableHand.Object.IsValid) return;

        if (tableVisuals.cards[0] == null)
        {
            for (int i = 0; i < 3; i++)
            {
                tableVisuals.cards[i] = SpawnCard(GetTableSlotPosition(i), GetTableSlotRotation(), i, true);
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (tableVisuals.cards[i] != null && !tableVisuals.isHiding[i])
                {
                    tableVisuals.cards[i].transform.position = GetTableSlotPosition(i);
                    tableVisuals.cards[i].transform.rotation = GetTableSlotRotation();
                }
            }
        }

        CardData[] currentData = new CardData[3];
        for (int i = 0; i < 3; i++)
        {
            currentData[i] = tableHand.tableCards[i];
        }

        DetectAndAnimateChanges(tableVisuals, currentData, true, default, true);
    }

    private void UpdatePlayerCards()
    {
        var allHands = PlayerHand.ActiveHands;
        HashSet<NetworkBehaviourId> activeHandIds = new HashSet<NetworkBehaviourId>();

        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object == null || !hand.Object.IsValid) continue;

            activeHandIds.Add(hand.Id);

            if (!playerVisualsMap.TryGetValue(hand.Id, out HandVisuals visuals))
            {
                visuals = new HandVisuals();
                bool isLocal = hand.Object.HasInputAuthority;

                for (int i = 0; i < 3; i++)
                {
                    visuals.cards[i] = SpawnCard(GetPlayerSlotPosition(hand, i, isLocal), GetPlayerSlotRotation(hand, isLocal), i, false);
                }

                playerVisualsMap.Add(hand.Id, visuals);
            }

            PlayerStatus status = hand.GetComponent<PlayerStatus>();
            bool isDead = status != null && status.lives <= 0;
            if (isDead)
            {
                for (int i = 0; i < 3; i++)
                    if (visuals.cards[i] != null && visuals.cards[i].gameObject.activeSelf)
                        visuals.cards[i].gameObject.SetActive(false);
                continue;
            }
            for (int i = 0; i < 3; i++)
                if (visuals.cards[i] != null && !visuals.cards[i].gameObject.activeSelf && !visuals.isHiding[i])
                    visuals.cards[i].gameObject.SetActive(true);

            bool isLocalPlayer = hand.Object.HasInputAuthority;

            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
            bool isTheirTurn = !isLocalPlayer && gameManager != null && gameManager.Object != null && gameManager.Object.IsValid && gameManager.isPlayersTurn(hand.Id);

            CardData[] currentData = new CardData[3];
            for (int i = 0; i < 3; i++)
            {
                currentData[i] = hand.myCards[i];
                if (visuals.cards[i] != null)
                {
                    visuals.cards[i].SetSelected(isLocalPlayer && hand.selectedCardIndex == i);
                    visuals.cards[i].SetTurnHighlight(isTheirTurn);
                }
            }

            DetectAndAnimateChanges(visuals, currentData, false, hand.Id, isLocalPlayer);
        }

        List<NetworkBehaviourId> toRemove = new List<NetworkBehaviourId>();
        foreach (var key in playerVisualsMap.Keys)
        {
            if (!activeHandIds.Contains(key))
            {
                toRemove.Add(key);
            }
        }

        foreach (var key in toRemove)
        {
            if (playerVisualsMap.TryGetValue(key, out HandVisuals visuals))
            {
                for (int i = 0; i < 3; i++)
                {
                    if (visuals.cards[i] != null)
                    {
                        Destroy(visuals.cards[i].gameObject);
                    }
                }
                playerVisualsMap.Remove(key);
            }
        }
    }

    private void DetectAndAnimateChanges(HandVisuals visuals, CardData[] currentData, bool isTable, NetworkBehaviourId handId, bool showFace)
    {
        for (int i = 0; i < 3; i++)
        {
            CardData oldCard = visuals.cachedData[i];
            CardData newCard = currentData[i];

            if (oldCard.number != newCard.number || oldCard.color != newCard.color)
            {
                visuals.cachedData[i] = newCard;

                if (oldCard.number != 0 && newCard.number != 0)
                {
                    TriggerSwapAnimation(isTable, handId, i, oldCard, newCard);
                }
                else if (newCard.number > 0)
                {
                    if (visuals.cards[i] != null)
                    {
                        TriggerDrawAnimation(visuals.cards[i], showFace ? newCard : default);
                    }
                }
                else
                {
                    if (visuals.cards[i] != null)
                    {
                        visuals.cards[i].SetCardData(showFace ? newCard : default);
                    }
                }
            }
            else if (!visuals.isHiding[i])
            {
                if (visuals.cards[i] != null)
                {
                    visuals.cards[i].SetCardData(showFace ? newCard : default);
                }
            }
        }
    }

    private void TriggerSwapAnimation(bool targetIsTable, NetworkBehaviourId targetHandId, int targetSlotIndex, CardData oldCard, CardData newCard)
    {
        if (targetIsTable)
        {
            var allHands = PlayerHand.ActiveHands;
            foreach (PlayerHand hand in allHands)
            {
                if (hand.Object == null || !hand.Object.IsValid) continue;

                int matchedHandIndex = -1;
                for (int idx = 0; idx < 3; idx++)
                {
                    if (hand.myCards[idx].number == oldCard.number && hand.myCards[idx].color == oldCard.color)
                    {
                        matchedHandIndex = idx;
                        break;
                    }
                }

                if (matchedHandIndex != -1)
                {
                    bool isLocal = hand.Object.HasInputAuthority;
                    AnimateSwapFlight(hand.Id, matchedHandIndex, targetSlotIndex, isLocal, newCard, oldCard);
                    break;
                }
            }
        }
    }

    private void AnimateSwapFlight(NetworkBehaviourId handId, int handIndex, int tableIndex, bool showFace, CardData oldHandCard, CardData oldTableCard)
    {
        if (tableHand == null || !playerVisualsMap.TryGetValue(handId, out HandVisuals handVisuals)) return;

        PhysicalCard handCard = handVisuals.cards[handIndex];
        PhysicalCard tableCard = tableVisuals.cards[tableIndex];

        if (handCard == null || tableCard == null) return;

        Vector3 startHandPos = handCard.VisualPivot.position;
        Vector3 endHandPos = handCard.transform.position;
        Vector3 startTablePos = tableCard.VisualPivot.position;
        Vector3 endTablePos = tableCard.transform.position;

        Quaternion startHandRot = handCard.VisualPivot.rotation;
        Quaternion endHandRot = handCard.transform.rotation;
        Quaternion startTableRot = tableCard.VisualPivot.rotation;
        Quaternion endTableRot = tableCard.transform.rotation;

        Vector3 startHandScale = handCard.transform.localScale;
        Vector3 startTableScale = tableCard.transform.localScale;

        handVisuals.isHiding[handIndex] = true;
        tableVisuals.isHiding[tableIndex] = true;

        handCard.gameObject.SetActive(false);
        tableCard.gameObject.SetActive(false);

        AudioManager.PlayCardSwap();

        GameObject flyHandToTable = Instantiate(physicalCardPrefab, startHandPos, startHandRot);
        GameObject flyTableToHand = Instantiate(physicalCardPrefab, startTablePos, startTableRot);

        PhysicalCard pFlyHandToTable = flyHandToTable.GetComponent<PhysicalCard>();
        PhysicalCard pFlyTableToHand = flyTableToHand.GetComponent<PhysicalCard>();

        pFlyHandToTable.IsInteractable = false;
        pFlyTableToHand.IsInteractable = false;
        pFlyHandToTable.SetCardData(oldHandCard);
        pFlyTableToHand.SetCardData(showFace ? oldTableCard : default);

        Tween.Custom(0f, 1f, flightDuration, t =>
        {
            if (flyHandToTable != null)
            {
                flyHandToTable.transform.position = Vector3.Lerp(startHandPos, endTablePos, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
                flyHandToTable.transform.rotation = Quaternion.Slerp(startHandRot, endTableRot, t);
                flyHandToTable.transform.localScale = Vector3.Lerp(startHandScale, startTableScale, t);
            }

            if (flyTableToHand != null)
            {
                flyTableToHand.transform.position = Vector3.Lerp(startTablePos, endHandPos, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
                flyTableToHand.transform.rotation = Quaternion.Slerp(startTableRot, endHandRot, t);
                flyTableToHand.transform.localScale = Vector3.Lerp(startTableScale, startHandScale, t);
            }
        }).OnComplete(() =>
        {
            Destroy(flyHandToTable);
            Destroy(flyTableToHand);

            if (handCard != null)
            {
                handCard.gameObject.SetActive(true);
                handCard.SetCardData(showFace ? oldTableCard : default);
            }
            if (tableCard != null)
            {
                tableCard.gameObject.SetActive(true);
                tableCard.SetCardData(oldHandCard);
            }

            handVisuals.isHiding[handIndex] = false;
            tableVisuals.isHiding[tableIndex] = false;
        });
    }

    private void TriggerDrawAnimation(PhysicalCard card, CardData data)
    {
        card.SetCardData(data);
        card.IsInteractable = false;
        Vector3 originalPos = card.transform.position;
        card.transform.position = originalPos + Vector3.up * 1f;
        Tween.Position(card.transform, originalPos, flightDuration, Ease.OutQuad)
            .OnComplete(() => { if (card != null) card.IsInteractable = true; });
        AudioManager.PlayCardDraw();
    }

    private PhysicalCard SpawnCard(Vector3 position, Quaternion rotation, int index, bool isTable)
    {
        GameObject cardObj = Instantiate(physicalCardPrefab, position, rotation);
        PhysicalCard card = cardObj.GetComponent<PhysicalCard>();
        card.CardIndex = index;
        card.IsTableCard = isTable;
        float scale = isTable ? 0.06f : 0.04f;
        cardObj.transform.localScale = new Vector3(scale, scale, scale);
        return card;
    }

    private Vector3 GetTableSlotPosition(int index)
    {
        Transform anchor = null;
        GameObject lookPoint = GameObject.Find("LookPoint");
        if (lookPoint != null)
        {
            anchor = lookPoint.transform;
        }
        else if (tableHand != null)
        {
            anchor = tableHand.transform;
        }

        if (anchor == null) return Vector3.zero;

        Quaternion localRot = anchor.rotation;
        if (PlayerHand.localHand != null)
        {
            float playerYaw = PlayerHand.localHand.transform.eulerAngles.y;
            localRot = Quaternion.Euler(anchor.eulerAngles.x, playerYaw, anchor.eulerAngles.z);
        }

        float offset = (index - 1) * tableCardSpacing;
        return anchor.position + localRot * new Vector3(offset, 0.05f, 0f);
    }

    private Quaternion GetTableSlotRotation()
    {
        Transform anchor = null;
        GameObject lookPoint = GameObject.Find("LookPoint");
        if (lookPoint != null)
        {
            anchor = lookPoint.transform;
        }
        else if (tableHand != null)
        {
            anchor = tableHand.transform;
        }

        if (anchor == null) return Quaternion.identity;

        if (PlayerHand.localHand != null)
        {
            float playerYaw = PlayerHand.localHand.transform.eulerAngles.y;
            return Quaternion.Euler(anchor.eulerAngles.x, playerYaw, anchor.eulerAngles.z);
        }
        return anchor.rotation;
    }

    private Vector3 GetPlayerSlotPosition(PlayerHand hand, int index, bool isLocal)
    {
        float offset = (index - 1) * cardSpacing;
        if (isLocal)
        {
            return hand.transform.TransformPoint(new Vector3(offset, localPlayerYOffset, localPlayerZOffset));
        }
        else
        {
            return hand.transform.TransformPoint(new Vector3(offset, remotePlayerYOffset, remotePlayerZOffset));
        }
    }

    private Quaternion GetPlayerSlotRotation(PlayerHand hand, bool isLocal)
    {
        if (isLocal)
        {
            return hand.transform.rotation * Quaternion.Euler(45f, 0f, 0f);
        }
        else
        {
            return hand.transform.rotation;
        }
    }
}
