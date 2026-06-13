using UnityEngine;

public class ThreeDCardRaycaster : MonoBehaviour
{
    [SerializeField] private LayerMask cardLayer = ~0;

    private Camera cachedCamera;
    private PhysicalCard currentHoveredCard;

    private void Start()
    {
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
    }

    private void Update()
    {
        var localHand = PlayerHand.localHand;
        if (localHand == null)
        {
            ClearHover();
            return;
        }

        Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);
        PhysicalCard hitCard = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, cardLayer))
        {
            PhysicalCard candidate = hit.collider.GetComponentInParent<PhysicalCard>();
            if (candidate != null && candidate.IsInteractable)
                hitCard = candidate;
        }

        if (hitCard != currentHoveredCard)
        {
            ClearHover();
            currentHoveredCard = hitCard;
            if (currentHoveredCard != null)
            {
                currentHoveredCard.SetHovered(true);
            }
        }

        if (currentHoveredCard != null && Input.GetMouseButtonDown(0))
        {
            AudioManager.PlayButtonClick();
            if (currentHoveredCard.IsTableCard)
            {
                localHand.selectTableCard(currentHoveredCard.CardIndex);
            }
            else
            {
                localHand.selectMyCard(currentHoveredCard.CardIndex);
            }
        }
    }

    private void ClearHover()
    {
        if (currentHoveredCard != null)
        {
            currentHoveredCard.SetHovered(false);
            currentHoveredCard = null;
        }
    }

    private void OnDisable()
    {
        ClearHover();
    }
}
