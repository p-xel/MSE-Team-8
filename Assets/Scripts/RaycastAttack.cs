using Fusion;
using UnityEngine;

public class RaycastAttack : NetworkBehaviour
{
    public float Damage = 10;
    public PlayerMovement playerMovement;

    public void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (HasStateAuthority == false) return;

        if (playerMovement == null || playerMovement.Camera == null) return;

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = playerMovement.Camera.ScreenPointToRay(Input.mousePosition);

            Vector3 debugLine = ray.direction * 50f;
            Debug.DrawRay(ray.origin, debugLine, Color.red, 2f);

            if (Physics.Raycast(ray, out var hit, 100f))
            {
                Debug.Log("Hit: " + hit.collider.name);
                if (hit.transform.TryGetComponent<Health>(out var health))
                {
                    health.DealDamageRpc(Damage);
                }
            }
        }
    }
}