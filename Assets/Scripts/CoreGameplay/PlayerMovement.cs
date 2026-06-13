using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private float _pitch;
    private float _yaw;

    [Header("Screen Zones (0 to 1)")]
    [Tooltip("Trigger left view if mouse X is below this (0.2 = left 20%)")]
    public float leftZoneThreshold = 0.2f;
    [Tooltip("Trigger right view if mouse X is above this (0.8 = right 20%)")]
    public float rightZoneThreshold = 0.8f;
    [Tooltip("Trigger bottom view if mouse Y is below this (0.2 = bottom 20%)")]
    public float bottomZoneThreshold = 0.2f;

    [Header("Preset Angles")]
    [Tooltip("Default Pitch when looking center")]
    public float centerPitch = 20f;
    
    [Tooltip("Yaw offset when looking left")]
    public float leftYawOffset = -45f;
    public float leftPitch = 15f;
    
    [Tooltip("Yaw offset when looking right")]
    public float rightYawOffset = 45f;
    public float rightPitch = 15f;
    
    [Tooltip("Yaw offset when looking down at cards")]
    public float bottomYawOffset = 0f;
    public float bottomPitch = 60f;

    [Tooltip("How fast the camera moves to the new zone")]
    public float transitionSpeed = 8f;

    private float _targetPitch;
    private float _targetYaw;
    private float _baseYaw; 

    [Header("Camera Setup")]
    public Camera Camera;
    public Transform cameraPivot;
    public Transform cameraHandle;

    [Header("Camera Shake")]
    public float shakeDuration = 0.35f;
    public float shakeMagnitude = 0.12f;
    private float _shakeTimer;
    private int _lastLives = -1;

    [Header("Death Setup")]
    public Transform deadTransformOverride;
    public Transform playerVisualRoot;
    private PlayerStatus _playerStatus;
    private Vector3 _initialVisualLocalPos;
    private Quaternion _initialVisualLocalRot;

    [Header("Character Selection")]
    public Transform playerBody;
    public GameObject characterPrefab1;
    public GameObject characterPrefab2;
    public GameObject characterPrefab3;
    public GameObject characterPrefab4;

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    private int characterIndex { get; set; }

    [Networked]
    private Vector3 networkedPosition { get; set; }

    [Networked]
    private Quaternion networkedRotation { get; set; }

    private void Awake()
    {
        if (Camera == null) Camera = Camera.main;

        if (cameraPivot == null || cameraHandle == null)
        {
            Debug.LogError($"[PlayerMovement] Camera components missing on {gameObject.name}! Please create 'CameraPivot' child with 'CameraHandle' sub-child.");
        }

        _playerStatus = GetComponent<PlayerStatus>();
        if (_playerStatus == null) _playerStatus = GetComponentInParent<PlayerStatus>();
        if (_playerStatus == null) _playerStatus = GetComponentInChildren<PlayerStatus>();

        if (playerVisualRoot != null)
        {
            _initialVisualLocalPos = playerVisualRoot.localPosition;
            _initialVisualLocalRot = playerVisualRoot.localRotation;
        }
    }

    public override void Spawned()
    {
        _baseYaw = transform.eulerAngles.y;
        _yaw = _baseYaw;
        _pitch = centerPitch;

        if (Object.HasStateAuthority)
        {
            characterIndex = Random.Range(0, 4);
            networkedPosition = transform.position;
            networkedRotation = transform.rotation;
        }
        UpdateCharacterModel();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.GetVisible()) return;

        if (Object.HasStateAuthority)
        {
            networkedPosition = transform.position;
            networkedRotation = transform.rotation;
        }

        if (!HasInputAuthority) return;

        float screenX = Input.mousePosition.x / Screen.width;
        float screenY = Input.mousePosition.y / Screen.height;

        if (screenY < bottomZoneThreshold)
        {
            _targetYaw = _baseYaw + bottomYawOffset;
            _targetPitch = bottomPitch;
        }
        else if (screenX < leftZoneThreshold)
        {
            _targetYaw = _baseYaw + leftYawOffset;
            _targetPitch = leftPitch;
        }
        else if (screenX > rightZoneThreshold)
        {
            _targetYaw = _baseYaw + rightYawOffset;
            _targetPitch = rightPitch;
        }
        else
        {
            _targetYaw = _baseYaw;
            _targetPitch = centerPitch;
        }

        float dt = Runner.DeltaTime;
        _yaw = Mathf.Lerp(_yaw, _targetYaw, transitionSpeed * dt);
        _pitch = Mathf.Lerp(_pitch, _targetPitch, transitionSpeed * dt);
    }

    public void LateUpdate()
    {
        if (Object == null || !Object.HasInputAuthority || !Runner.GetVisible())
            return;

        if (_playerStatus == null) _playerStatus = GetComponent<PlayerStatus>();
        if (_playerStatus != null)
        {
            int lives = _playerStatus.lives;
            if (_lastLives >= 0 && lives < _lastLives) _shakeTimer = shakeDuration;
            _lastLives = lives;
        }

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(_pitch, _yaw - _baseYaw, 0f);
        }

        if (Camera != null && cameraHandle != null)
        {
            Camera.transform.SetPositionAndRotation(cameraHandle.position, cameraHandle.rotation);

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                float amt = shakeMagnitude * Mathf.Clamp01(_shakeTimer / shakeDuration);
                Camera.transform.position += Random.insideUnitSphere * amt;
            }
        }
    }

    public override void Render()
    {
        if (Object == null || !Object.IsValid) return;

        if (!Object.HasStateAuthority && networkedPosition != Vector3.zero)
        {
            transform.position = networkedPosition;
            transform.rotation = networkedRotation;
        }

        if (_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
            if (_playerStatus == null) _playerStatus = GetComponentInParent<PlayerStatus>();
            if (_playerStatus == null) _playerStatus = GetComponentInChildren<PlayerStatus>();
        }

        if (_playerStatus != null && _playerStatus.lives <= 0)
        {
            SeatedLookAtIK ik = GetComponent<SeatedLookAtIK>();
            if (ik == null) ik = GetComponentInChildren<SeatedLookAtIK>();
            if (ik != null && ik.enabled) ik.enabled = false;

            Animator animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.enabled) animator.enabled = false;

            if (deadTransformOverride != null && playerVisualRoot != null)
            {
                playerVisualRoot.position = deadTransformOverride.position;
                playerVisualRoot.rotation = deadTransformOverride.rotation;
            }

            if (HasInputAuthority)
            {
                OrbitCamera orbitCam = FindAnyObjectByType<OrbitCamera>();
                if (orbitCam != null && !orbitCam.enabled)
                {
                    orbitCam.enabled = true;
                }
            }
        }
        else
        {
            SeatedLookAtIK ik = GetComponent<SeatedLookAtIK>();
            if (ik == null) ik = GetComponentInChildren<SeatedLookAtIK>();
            if (ik != null && !ik.enabled) ik.enabled = true;

            Animator animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && !animator.enabled) animator.enabled = true;

            if (playerVisualRoot != null && playerVisualRoot.localPosition != _initialVisualLocalPos)
            {
                playerVisualRoot.localPosition = _initialVisualLocalPos;
                playerVisualRoot.localRotation = _initialVisualLocalRot;
            }
        }
    }

    private void OnCharacterIndexChanged()
    {
        UpdateCharacterModel();
    }

    private void UpdateCharacterModel()
    {
        if (playerBody == null)
        {
            playerBody = transform.Find("PlayerBody");
        }

        if (playerBody == null) return;

        foreach (Transform child in playerBody)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        GameObject prefabToSpawn = null;
        switch (characterIndex)
        {
            case 0: prefabToSpawn = characterPrefab1; break;
            case 1: prefabToSpawn = characterPrefab2; break;
            case 2: prefabToSpawn = characterPrefab3; break;
            case 3: prefabToSpawn = characterPrefab4; break;
        }

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, playerBody);
        }

        if (Object.HasInputAuthority)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

        SeatedLookAtIK ik = GetComponent<SeatedLookAtIK>();
        if (ik == null) ik = GetComponentInChildren<SeatedLookAtIK>();
        if (ik != null)
        {
            ik.RefreshAnimator();
        }
    }
}
