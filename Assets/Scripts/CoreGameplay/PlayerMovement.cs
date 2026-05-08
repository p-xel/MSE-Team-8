using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private bool _jumpPressed;
    private CharacterController _controller;
    private float _verticalVelocity;
    private float _pitch;
    private float _yaw;

    public float playerSpeed = 5f;
    public float jumpForce = 10f;
    public float lookSensitivity = 2f;

    [Header("Camera Setup")]
    public Camera Camera;
    public Transform cameraPivot;
    public Transform cameraHandle;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (Camera == null) Camera = Camera.main;

        if (cameraPivot == null || cameraHandle == null)
        {
            Debug.LogError($"[PlayerMovement] Camera components missing on {gameObject.name}! Please create 'CameraPivot' child with 'CameraHandle' sub-child.");
        }
    }

    public override void Spawned()
    {
        _yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (!Runner.GetVisible()) return;

        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.GetVisible()) return;

        if (HasStateAuthority == false && HasInputAuthority == false) return;

        float dt = Runner.DeltaTime;

        _pitch = Mathf.Clamp(_pitch + Input.GetAxisRaw("Mouse Y") * lookSensitivity * -1f, -89f, 89f);
        _yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = transform.rotation * inputDirection.normalized * playerSpeed;

        if (_controller.isGrounded)
        {
            // small negative holds the controller against the ground each tick
            _verticalVelocity = _jumpPressed ? jumpForce : -2f;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * 4f * dt;
        }

        moveVelocity.y = _verticalVelocity;
        _controller.Move(moveVelocity * dt);

        _jumpPressed = false;
    }

    public void LateUpdate()
    {
        if (Object == null || !Object.HasInputAuthority || !Runner.GetVisible())
            return;

        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        Camera.main.transform.SetPositionAndRotation(cameraHandle.position, cameraHandle.rotation);
    }
}
