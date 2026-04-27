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

        // validation to prevent the NullReferenceException 
        if (cameraPivot == null || cameraHandle == null)
        {
            Debug.LogError($"[PlayerMovement] Camera components missing on {gameObject.name}! Please create 'CameraPivot' child with 'CameraHandle' sub-child.");
        }
    }

    public override void Spawned()
    {
        // SimpleKCC SetGravity expects a float for the Y-axis gravity multiplier (or direct value)
        _simpleKCC.SetGravity(Physics.gravity.y * 4.0f);
    }


    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // in simple scenarios (especially Shared Mode), restrict this to the local authority
        if (HasStateAuthority == false && HasInputAuthority == false) return;

        // process look rotation
        float lookX = Input.GetAxisRaw("Mouse Y") * lookSensitivity * -1f; // Pitch (up/down)
        float lookY = Input.GetAxisRaw("Mouse X") * lookSensitivity;       // Yaw (left/right)
        _simpleKCC.AddLookRotation(new Vector2(lookX, lookY));

        // process movement
        Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = _simpleKCC.TransformRotation * inputDirection.normalized * playerSpeed;

        // process jump
        float jumpImpulse = 0f;
        if (_jumpPressed && _simpleKCC.IsGrounded)
        {
            // SimpleKCC Move expects a float for jumpImpulse in its latest version
            jumpImpulse = jumpForce;
        }

        _simpleKCC.Move(moveVelocity, jumpImpulse);

        _jumpPressed = false;
    }


    public void LateUpdate()
    {
        // only input authority needs to update camera.
        if (Object == null || Object.HasInputAuthority == false)
            return;

        // update camera pivot and transfer properties from camera handle to Main Camera.
        // LateUpdate() is called after all Render() calls - the character is already interpolated.

        Vector2 pitchRotation = _simpleKCC.GetLookRotation(true, false);
        cameraPivot.localRotation = Quaternion.Euler(pitchRotation);

        Camera.main.transform.SetPositionAndRotation(cameraHandle.position, cameraHandle.rotation);
    }
}