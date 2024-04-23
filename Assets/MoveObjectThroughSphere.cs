using System.Collections;
using System.Text.RegularExpressions;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class RandomMovementOnSphericalSurface : MonoBehaviour
{
    public float lookThreshold = 2f; // Adjust as needed
    public float triggertime = 1f; //Time needed to trigger the object movement
    private float sphereRadius = 25f; // Radius of the sphere
    public float movementSpeed = 10f; // Speed of movement
    public float minChangeDirectionInterval = 2f; // Minimum time between direction changes
    public float maxChangeDirectionInterval = 4f; // Maximum time between direction changes
    public float minAngle = 30f; // Minimum angle for upward direction (degrees)
    public float maxAngle = 150f; // Maximum angle for upward direction (degrees)

    private bool isLooking = false;
    private bool triggered = false;
    private float lookTimeElapsed = 0f;
    private Vector3 moveDirection; // Current movement direction
    private float nextDirectionChangeTime; // Time for next direction change
    private int difficultyvalue;
    private bool isFireCooldown = false;
    private bool isHoveringTheObject = false;
    private bool isResetting = false;
    public float fireCooldownDuration = 1f; // Cooldown duration for firing (in seconds)
    Animator animator;

    public InputActionReference TriggerR;
    public InputActionReference AimDirR;
    bool ShootTriggerPressed;
    bool ShootTriggerPressing;


    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        // Initialize a random movement direction
        moveDirection = Random.onUnitSphere.normalized;
        // Set the initial time for the next direction change
        nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);

        movementSpeed = PlayerPrefs.GetInt("ObjectSpeed", 0) != 0 ? PlayerPrefs.GetInt("ObjectSpeed", 0) : movementSpeed;

        difficultyvalue = PlayerPrefs.GetInt("Difficulty", 0);
        if (difficultyvalue == 0) sphereRadius = 10f;
        else if (difficultyvalue == 1) sphereRadius = 20f;
        else {sphereRadius = 25f;}

    }


    void Update()
    {
        CheckLooking();

        //Shoot
        ShootTriggerPressed = (TriggerR.action.ReadValue<float>() > 0.5f) && !ShootTriggerPressing ? true : false;
        ShootTriggerPressing = (TriggerR.action.ReadValue<float>() > 0.5f) ? true : false;

        if (ShootTriggerPressed)
        {
            Fire();
        }

        //CheckObjectRotation();

        // Move the object along the spherical path and randomly change direction
        MoveObjectOnSphericalPath();
    }

    public void Object_Hover_Entered() {isHoveringTheObject=true;}
    public void Object_Hover_Exited() {isHoveringTheObject=false;}

    public void Fire()
    {
        // If not in cooldown, register hit and start cooldown
        if (!isFireCooldown && isHoveringTheObject)
        {
            ResetObject();
        }
        // Start cooldown
        StartCoroutine(FireCooldown());
    }

    IEnumerator FireCooldown()
    {
        // Set cooldown flag to true
        isFireCooldown = true;

        // Wait for half a second
        yield return new WaitForSeconds(0.5f);

        // Reset cooldown flag to false after the specified duration
        yield return new WaitForSeconds(fireCooldownDuration - 0.5f);
        isFireCooldown = false;
    }

    private void ResetObject()
    {
        // Set the flag to indicate resetting is in progress
        if (!isResetting)
        { 
            isResetting = true;
            LookAroundLoop();

            // Wait for 2 seconds
            //yield return new WaitForSeconds(2f);

            // Apply effect to the object's current location
            //ApplyEffect(obj.transform.position);

            // Move the object to a random point and make it stationary
            transform.position = GetRandomPoint();
            transform.rotation = Quaternion.identity;
            triggered = false;

            // Reset the flag to indicate resetting is done
            isResetting = false;
        }
    }

    public void ChangeLevel()
    {
        // Implement teleportation logic here
        Debug.Log("Teleporting to next level...");
        // Get the current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Extract the number part of the scene name using regular expression
        Match match = Regex.Match(currentSceneName, @"\d+");
        if (match.Success)
        {
            // Parse the extracted digits
            int sceneNumber = int.Parse(match.Value);

            // Increment the scene number
            sceneNumber++;

            // Form the new scene name
            string nextSceneName = currentSceneName.Replace(match.Value, sceneNumber.ToString());

            // Load the next scene
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("Scene name doesn't contain a number.");
        }
    }

    void CheckObjectRotation()
    {
        // Get the rotation of the object
        Vector3 rotation = transform.rotation.eulerAngles;

        // Check if any rotation values are different from 0
        bool isRotating = rotation.x != 0 || rotation.y != 0 || rotation.z != 0;

        // If the object is rotating
        if (isRotating)
        {
            // Trigger the Fire function
            Fire();
        }
    }
    
    // Check if the user is looking at the cube
    void CheckLooking()
    {
        if (XRSettings.isDeviceActive)
        {
            // Use XR input to determine if the user is looking at the cube
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 headPosition);
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion headRotation);

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
            if (lookTimeElapsed >= triggertime)
            {
                triggered = true;
                StartFlight();
            }
        }
        else
        {
            lookTimeElapsed = 0f;
        }
    }

    
    // Move the cube through the spherical path
    void MoveObjectOnSphericalPath()
    {
        if (triggered)
        {
            // Set the rotation of the cube to (0, 0, 0) and the position positive
            transform.rotation = Quaternion.identity;
            if (transform.position.y < 0)
                transform.position = new Vector3(transform.position.x, 0.000001f, transform.position.z);

            // Move the object in the current direction
            transform.Translate(moveDirection * movementSpeed * Time.deltaTime, Space.World);

            // Ensure the cube stays on the surface of the sphere
            Vector3 newPosition = transform.position.normalized * sphereRadius;

            // If the cube's position is negative on the y-axis, adjust the movement direction
            if (newPosition.y < 0)
            {
                float angle = Random.Range(minAngle, maxAngle);
                float radians = angle * Mathf.Deg2Rad;
                moveDirection = new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f);
            }

            //Rotate the object
            transform.Rotate(moveDirection);

            transform.position = newPosition;

            // Check if it's time to change direction
            if (Time.time >= nextDirectionChangeTime)
            {
                // Change direction
                ChangeDirection();
                // Set the next time for direction change
                nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);
            }
        }
    }

    // Change the movement direction to a random direction
    void ChangeDirection()
    {
        moveDirection = Random.onUnitSphere.normalized;
    }

    Vector3 GetRandomPoint()
    {
        // Generate random point in a unit sphere
        Vector3 randomDirection = Random.insideUnitSphere;

        // Scale the random point to the sphere's radius
        Vector3 randomPoint = randomDirection * sphereRadius;

        // Ensure y coordinate is greater than 0
        if (randomPoint.y <= 0)
        {
            // Adjust y coordinate if it's less than or equal to 0
            randomPoint.y = Mathf.Abs(randomPoint.y) + 0.0001f;
        }

        return randomPoint;
    }

    public void StartFlight() { animator.SetTrigger("TakeOffTrigger"); animator.ResetTrigger("LookAroundTrigger"); animator.ResetTrigger("IdleTrigger"); }
    public void LookAroundLoop() { animator.SetTrigger("LookAroundTrigger"); animator.ResetTrigger("TakeOffTrigger"); animator.ResetTrigger("IdleTrigger"); }
    public void Idle() { animator.SetTrigger("IdleTrigger"); animator.ResetTrigger("TakeOffTrigger"); animator.ResetTrigger("IdleTrigger"); }
}
