using UnityEngine;

public class SeatedLookAtIK : MonoBehaviour
{
    private Animator animator;

    public Camera targetCamera;
    public float lookDistance = 10f;
    public float lookWeight = 1f;
    public float bodyWeight = 0.2f;
    public float headWeight = 0.8f;
    public float clampWeight = 0.5f;

    private void Awake()
    {
        RefreshAnimator();
    }

    public void RefreshAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            // If the Animator is on a child object, attach an IK proxy to forward callbacks to the parent.
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                var proxy = animator.gameObject.GetComponent<SeatedLookAtIKProxy>();
                if (proxy == null)
                {
                    proxy = animator.gameObject.AddComponent<SeatedLookAtIKProxy>();
                }
                proxy.Initialize(this);
            }
        }
    }

    private void Start()
    {
        FindPlayerCamera();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator != null && animator.gameObject == gameObject)
        {
            OnAnimatorIKCallback(layerIndex);
        }
    }

    public void OnAnimatorIKCallback(int layerIndex)
    {
        if (animator == null) return;

        if (targetCamera == null)
        {
            FindPlayerCamera();
        }

        if (targetCamera != null)
        {
            Vector3 lookPosition;
            var netObj = GetComponentInParent<Fusion.NetworkObject>();
            if (netObj == null) netObj = GetComponentInChildren<Fusion.NetworkObject>();

            // Local player looks down their camera view ray, while remote players look directly at the player camera position.
            if (netObj != null && netObj.HasInputAuthority)
            {
                lookPosition = targetCamera.transform.position + targetCamera.transform.forward * lookDistance;
            }
            else
            {
                lookPosition = targetCamera.transform.position;
            }

            animator.SetLookAtWeight(lookWeight, bodyWeight, headWeight, 0f, clampWeight);
            animator.SetLookAtPosition(lookPosition);
        }
        else
        {
            animator.SetLookAtWeight(0f);
        }
    }

    private void FindPlayerCamera()
    {
        if (targetCamera != null) return;

        // Resolve the camera reference.
        var movement = GetComponentInParent<PlayerMovement>();
        if (movement == null)
        {
            movement = GetComponentInChildren<PlayerMovement>();
        }

        if (movement != null && movement.Camera != null)
        {
            targetCamera = movement.Camera;
        }
        else
        {
            targetCamera = GetComponentInChildren<Camera>();
            if (targetCamera == null)
            {
                targetCamera = GetComponentInParent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }
}

public class SeatedLookAtIKProxy : MonoBehaviour
{
    private SeatedLookAtIK parentIK;

    public void Initialize(SeatedLookAtIK parent)
    {
        parentIK = parent;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (parentIK != null)
        {
            parentIK.OnAnimatorIKCallback(layerIndex);
        }
    }
}
