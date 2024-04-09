using UnityEngine;
using UnityEngine.XR;

public class LookAndHoverRandomly : MonoBehaviour
{
    public float lookThreshold = 0.5f; // Adjust as needed
    public float movementSpeed = 100f; // Speed of movement
    public float sphereRadius = 25f; // Radius of the sphere
    public float minChangeDirectionInterval = 2f; // Minimum time between direction changes
    public float maxChangeDirectionInterval = 5f; // Maximum time between direction changes

    private bool isLooking = false;
    private bool triggered = false;
    private float lookTimeElapsed = 0f;
    private Vector3 targetPosition;
    private Vector3 moveDirection; // Current movement direction
    private float nextDirectionChangeTime; // Time for next direction change

    void Start()
    {
        // Initialize target position randomly on the surface of the sphere
        targetPosition = GetRandomPointOnSphereSurface();
        // Initialize a random movement direction
        moveDirection = Random.onUnitSphere.normalized;
        // Set the initial time for the next direction change
        nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);
    }

    void Update()
    {
        CheckLooking();

        // Move the object along the spherical path and randomly change direction
        MoveObjectOnSphericalPath();
    }

    // Check if the user is looking at the cube
    void CheckLooking()
    {
        if (XRSettings.isDeviceActive)
        {
            // Use XR input to determine if the user is looking at the cube
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);

            Vector3 forwardDirection = headRotation * Vector3.forward;
            Vector3 toCube = transform.position - headPosition;
            float angle = Vector3.Angle(forwardDirection, toCube);

            isLooking = angle < lookThreshold;
        }
        else
        {
            // Simulate gaze direction using mouse position in the editor or XR device simulator
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                isLooking = hit.collider.gameObject == gameObject;
            }
            else
            {
                isLooking = false;
            }
        }

        // If the user has been looking at the cube for more than 3 seconds
        if (isLooking)
        {
            lookTimeElapsed += Time.deltaTime;
            if (lookTimeElapsed >= 3f)
            {
                triggered = true;
            }
        }
        else
        {
            lookTimeElapsed = 0f;
        }
    }

    // Move the object along the spherical path and randomly change direction
    void MoveObjectOnSphericalPath()
    {
        if (triggered)
        {
            // Calculate the movement distance based on movement speed
            float step = movementSpeed * Time.deltaTime;

            // Move the cube along the spherical path
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // Check if the cube has reached the target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                // Calculate a new random direction within the upper hemisphere
                moveDirection = Random.onUnitSphere.normalized;

                // Update the target position along the spherical path
                targetPosition = GetRandomPointOnSphereSurface();

                // Set the next time for direction change
                nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);
            }
        }
    }

    // Get a random point on the surface of the sphere
    Vector3 GetRandomPointOnSphereSurface()
    {
        Vector3 randomDirection = Random.onUnitSphere.normalized;
        randomDirection.y = Mathf.Abs(randomDirection.y); // Ensure positive y value for upper hemisphere
        return randomDirection * sphereRadius;
    }
}
