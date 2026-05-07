using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private bool _jumpPressed;

    private SimpleKCC _simpleKCC;

    public float playerSpeed = 5f;
    public float jumpForce = 10f;
    public float lookSensitivity = 2f;

    [Header("Camera Setup")]
    public Camera Camera;
    public Transform cameraPivot;
    public Transform cameraHandle;

    private void Awake()
    {
        _simpleKCC = GetComponent<SimpleKCC>();

        if (Camera == null) Camera = Camera.main;

        if (cameraPivot == null || cameraHandle == null)
        {
            Debug.LogError($"[PlayerMovement] Camera components missing on {gameObject.name}! Please create 'CameraPivot' child with 'CameraHandle' sub-child.");
        }
    }

    public override void Spawned()
    {
        // SimpleKCC.SetGravity takes a Y-axis value, not a Vector3
        _simpleKCC.SetGravity(Physics.gravity.y * 4.0f);
    }


    private void Update()
    {
        // multi-peer: only the visible peer reads input
        if (!Runner.GetVisible()) return;

        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.GetVisible()) return;

        // Shared Mode: only the owning client drives its character
        if (HasStateAuthority == false && HasInputAuthority == false) return;

        float lookX = Input.GetAxisRaw("Mouse Y") * lookSensitivity * -1f; // pitch
        float lookY = Input.GetAxisRaw("Mouse X") * lookSensitivity;       // yaw
        _simpleKCC.AddLookRotation(new Vector2(lookX, lookY));

        Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = _simpleKCC.TransformRotation * inputDirection.normalized * playerSpeed;

        float jumpImpulse = 0f;
        if (_jumpPressed && _simpleKCC.IsGrounded)
        {
            jumpImpulse = jumpForce;
        }

        _simpleKCC.Move(moveVelocity, jumpImpulse);

        _jumpPressed = false;
    }


    public void LateUpdate()
    {
        if (Object == null || !Object.HasInputAuthority || !Runner.GetVisible())
            return;

        // LateUpdate runs after Render() interpolation, so the KCC pose is already final
        Vector2 pitchRotation = _simpleKCC.GetLookRotation(true, false);
        cameraPivot.localRotation = Quaternion.Euler(pitchRotation);

        Camera.main.transform.SetPositionAndRotation(cameraHandle.position, cameraHandle.rotation);
    }
}
