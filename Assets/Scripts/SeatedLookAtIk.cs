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

    public bool showDebugLogs = false;
    private bool hasReceivedIKCallback = false;

    private void Awake()
    {
        RefreshAnimator();
    }

    public void RefreshAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
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
        ValidateSetup();
        StartCoroutine(CheckIKCallbackActive());
    }

    private void ValidateSetup()
    {
        Animator rootAnimator = GetComponent<Animator>();
        Animator childAnimator = GetComponentInChildren<Animator>();

        if (rootAnimator == null && childAnimator != null)
        {
            Debug.LogWarning($"[SeatedLookAtIK] Animator is on child {childAnimator.name}. Proxy has been attached to forward IK callbacks.");
        }
        else if (rootAnimator == null && childAnimator == null)
        {
            Debug.LogError($"[SeatedLookAtIK] No Animator found on this GameObject or children.");
        }
        else
        {
            if (rootAnimator != null && !rootAnimator.isHuman)
            {
                Debug.LogError($"[SeatedLookAtIK] Animator on {gameObject.name} is not Humanoid. LookAt IK requires a Humanoid avatar.");
            }
        }
    }

    private System.Collections.IEnumerator CheckIKCallbackActive()
    {
        yield return new WaitForSeconds(2f);
        if (!hasReceivedIKCallback && animator != null && animator.enabled)
        {
            Debug.LogWarning($"[SeatedLookAtIK] OnAnimatorIK is not being called. Make sure IK Pass is enabled on your Animator Controller layer.");
        }
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
        hasReceivedIKCallback = true;

        if (showDebugLogs)
        {
            Debug.Log($"[SeatedLookAtIK] OnAnimatorIKCallback: layer={layerIndex}, animator={animator.name}, camera={targetCamera != null}");
        }

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

        if (showDebugLogs)
        {
            Debug.Log($"[SeatedLookAtIK] FindPlayerCamera resolved targetCamera: {(targetCamera != null ? targetCamera.name : "null")}");
        }
    }

    private void OnDrawGizmos()
    {
        if (targetCamera != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 lookPosition;
            var netObj = GetComponentInParent<Fusion.NetworkObject>();
            if (netObj == null) netObj = GetComponentInChildren<Fusion.NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority)
            {
                lookPosition = targetCamera.transform.position + targetCamera.transform.forward * lookDistance;
            }
            else
            {
                lookPosition = targetCamera.transform.position;
            }

            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, lookPosition);
            Gizmos.DrawSphere(lookPosition, 0.2f);
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