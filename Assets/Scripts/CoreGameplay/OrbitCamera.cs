using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Tooltip("The object the camera will rotate around")]
    public Transform target;
    
    [Tooltip("How fast the camera rotates in degrees per second")]
    public float rotationSpeed = 30f;
    
    [Tooltip("Use smooth rotation instead of snapping immediately")]
    public bool smoothLookAt = false;
    public float lookSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.RotateAround(target.position, Vector3.up, rotationSpeed * Time.deltaTime);

        if (smoothLookAt)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
        }
        else
        {
            transform.LookAt(target);
        }
    }
}
