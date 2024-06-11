using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.VisualScripting;

public class RandomMovementOnSphericalSurface : MonoBehaviour
{
    public float lookThreshold = 2f; // Adjust as needed
    public float triggerTime = 1f; //Time needed to trigger the object movement
    private float sphereRadius = 25f; // Radius of the sphere
    public float movementSpeed = 10f; // Speed of movement
    public float minChangeDirectionInterval = 2f; // Minimum time between direction changes
    public float maxChangeDirectionInterval = 4f; // Maximum time between direction changes
    public float minAngle = 30f; // Minimum angle for upward direction (degrees)
    public float maxAngle = 150f; // Maximum angle for upward direction (degrees)
    public TextMeshProUGUI Score_Label;
    public TextMeshProUGUI WinScore_Label;
    public TextMeshProUGUI Timer_Label; //Timer label
    private bool Timer_Running;
    private float Timer_Time;
    public int Timer_Mode = 0;
    public int Timer_Countdown_Time = 60;
    private string Timer_Formatted;
    private int Default_Win_Score = 10;
    public int Win_Score = 10;
    private int Map_Index = 0;
    public Material[] skyboxMaterials; // Assign your skybox materials in the inspector



    private bool isLooking = false;
    private bool triggered = false;
    private float lookTimeElapsed = 0f;
    private int Score = 0;
    private Vector3 moveDirection; // Current movement direction
    private float nextDirectionChangeTime; // Time for next direction change
    private int difficultyvalue;
    private bool isFireCooldown = false;
    private bool isHoveringTheObject = false;
    private bool isHoveringTheLobbyTeleport = false;
    private bool isResetting = false;
    public float fireCooldownDuration = 1f; // Cooldown duration for firing (in seconds)
    public float rotationSpeed = 1f;
    Animator animator;
    public new ParticleSystem particleSystem;

    public InputActionReference TriggerR;
    public InputActionReference AimDirR;
    public InputActionReference TriggerL;
    public InputActionReference AimDirL;
    public InputActionReference LobbyTrigger;
    bool ShootTriggerPressedR;
    bool ShootTriggerPressingR;
    bool ShootTriggerPressedL;
    bool ShootTriggerPressingL;
    bool TeleportToLobbyPressedR;

    private XRGrabInteractable grabInteractable;

    void Start()
    {
        Map_Index = PlayerPrefs.GetInt("MapIndex", 0);

        animator = gameObject.GetComponent<Animator>();
        grabInteractable = gameObject.GetComponent<XRGrabInteractable>();

        // Add event listeners for grab events
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);

        // Initialize a random movement direction
        moveDirection = Random.onUnitSphere.normalized;
        // Set the initial time for the next direction change
        nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);

        movementSpeed = PlayerPrefs.GetFloat("ObjectSpeed", 0) != 0 ? PlayerPrefs.GetFloat("ObjectSpeed", 0) : movementSpeed; //Override by gameobject if the user sets the speed to 0 in the game
        Timer_Mode = Timer_Mode == 1 ? Timer_Mode : PlayerPrefs.GetInt("TimerMode", 0); //Overide the timer mode by the gameobject if setted to 1
        Timer_Countdown_Time = PlayerPrefs.GetInt("CountdownTime", 0) <= 10 ? Timer_Countdown_Time : PlayerPrefs.GetInt("CountdownTime", 0); //Override by countdowntime if the value is 10 or less
        Win_Score = PlayerPrefs.GetInt("WinScore", 0) != 0 ? PlayerPrefs.GetInt("WinScore", 0) : Win_Score;
        //Debug.Log(PlayerPrefs.GetInt("CountdownTime", 0).ToString() + PlayerPrefs.GetInt("WinScore", 0).ToString());

        difficultyvalue = PlayerPrefs.GetInt("Difficulty", 0);
        if (difficultyvalue == 0) { sphereRadius = 10f; maxChangeDirectionInterval = 5; /*Timer_Countdown_Time = 60; Win_Score = 10; */}
        else if (difficultyvalue == 1) { sphereRadius = 20f; maxChangeDirectionInterval = 3; /*Timer_Countdown_Time = 30; Win_Score = 10; */}
        else { sphereRadius = 25f; maxChangeDirectionInterval = 2; /*Timer_Countdown_Time = 20; Win_Score = 10; */}

        WinScore_Label.text = "Meta: " + Win_Score.ToString();

        StartTimer(); //Start the Timer
    }

    public void StartTimer()
    {
        if (Timer_Mode == 0) Timer_Time = 0;
        else Timer_Time = Timer_Countdown_Time;
        Timer_Running = true;
    }

    public void StopTimer()
    {
        Timer_Running = false;
    }

    void Update()
    {
        CheckLooking();

        //ShootR
        ShootTriggerPressedR = (TriggerR.action.ReadValue<float>() > 0.5f) && !ShootTriggerPressingR ? true : false;
        ShootTriggerPressingR = (TriggerR.action.ReadValue<float>() > 0.5f) ? true : false;
        //ShootL
        ShootTriggerPressedL = (TriggerL.action.ReadValue<float>() > 0.5f) && !ShootTriggerPressingL ? true : false;
        ShootTriggerPressingL = (TriggerL.action.ReadValue<float>() > 0.5f) ? true : false;
        //TeleportToLobbyR
        TeleportToLobbyPressedR = (LobbyTrigger.action.ReadValue<float>() > 0.5f);
        

        if (ShootTriggerPressedR || ShootTriggerPressedL)
        {
            Fire();
        }



        if (TeleportToLobbyPressedR || (isHoveringTheLobbyTeleport && (ShootTriggerPressedR || ShootTriggerPressedL)))
        {
            SaveScores();
            TeleportToLobby();
        }

        //CheckObjectRotation();

        // Move the object along the spherical path and randomly change direction
        MoveObjectOnSphericalPath();

        if (Timer_Running)
        {
            if (Timer_Mode == 0)
            {
                Timer_Time += Time.deltaTime;
                if (Score >= Win_Score)
                {
                    PlayerPrefs.SetInt("ScoreSum", PlayerPrefs.GetInt("ScoreSum", 0) + Score);
                    PlayerPrefs.SetInt("TimeSum", PlayerPrefs.GetInt("TimeSum", 0) + (int)Timer_Time);

                    int randomnumber = Map_Index;
                    while (randomnumber == Map_Index) { randomnumber = Random.Range(1, 9); }
                    PlayerPrefs.SetInt("WinScore", Win_Score + 1);
                    ChangeLevel_SameScene(randomnumber);
                    PlayerPrefs.SetInt("MapIndex", randomnumber); return;
                }
            }
            else
            {
                Timer_Time -= Time.deltaTime;
                if (Timer_Time < 0)
                {
                    PlayerPrefs.SetInt("ScoreSum", PlayerPrefs.GetInt("ScoreSum", 0) + Score);
                    PlayerPrefs.SetInt("TimeSum", PlayerPrefs.GetInt("TimeSum", 0) + (int)Timer_Time);
                    if (Score < Win_Score) { PlayerPrefs.SetInt("WinScore", Default_Win_Score); TeleportToLobby(); return; }
                    else
                    {
                        int randomnumber = Map_Index;
                        while (randomnumber == Map_Index) { randomnumber = Random.Range(1, 9); }
                        PlayerPrefs.SetInt("WinScore", Win_Score + 1);
                        ChangeLevel_SameScene(randomnumber); 
                        PlayerPrefs.SetInt("MapIndex", randomnumber); return;
                    }
                }
            }

            int seconds = Mathf.FloorToInt(Timer_Time);
            int minutes = Mathf.FloorToInt(seconds / 60);
            int remainingseconds = seconds % 60;
            Timer_Formatted = string.Format("{0:0}:{1:00}", minutes, remainingseconds);
            //Debug.Log("timer " + Timer_Formatted);
            Timer_Label.text = Timer_Formatted;
        }
    }

    public void SaveScores()
    {
        PlayerPrefs.SetInt("ScoreSum", PlayerPrefs.GetInt("ScoreSum", 0) + Score);
        PlayerPrefs.SetInt("TimeSum", PlayerPrefs.GetInt("TimeSum", 0) + (int)Timer_Time);
    }

    public void Object_Hover_Entered() {isHoveringTheObject=true;}
    public void Object_Hover_Exited() {isHoveringTheObject=false;}
    public void TeleportSphere_Hover_Entered() { isHoveringTheLobbyTeleport = true; }
    public void TeleportSphere_Hover_Exited() { isHoveringTheLobbyTeleport = false; }

    public void Fire()
    {
        // If not in cooldown, register hit and start cooldown
        if (!isFireCooldown && isHoveringTheObject)
        {
            particleSystem.transform.position = transform.position;
            particleSystem.Play(); //Show Crow Exploding Animation
            Score += 1;
            Score_Label.text = "Pontos: " + Score.ToString();
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

    public void ChangeLevel_SameScene(int skybox_index)
    {
        // Load the skybox material
        Material skyboxMaterial = null;
        if (skybox_index >= 0 && skybox_index < skyboxMaterials.Length)
        {
            skyboxMaterial = skyboxMaterials[skybox_index];
        }
        else
        {
            Debug.LogWarning($"Skybox material for index {skybox_index} not found.");
        }

        // Teleport logic here
        SceneManager.LoadSceneAsync($"GameScene{/*skybox_index*/1}").completed += (operation) =>
        {
            // Apply the skybox material after the scene has finished loading
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                Debug.Log($"Skybox changed to {skyboxMaterial.name}.");
            }

            Debug.Log($"GameScene{/*skybox_index + */1} Loaded...");
        };
    }

    public void TeleportToLobby()
    {
        // Implement teleportation logic here
        Debug.Log("Teleporting to Lobby...");
        
        try
        {
            // Load the next scene
            SceneManager.LoadScene("Lobby");
        }
        catch
        { 
            Debug.Log("Teleporting failed!!!"); 
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
            if (lookTimeElapsed >= triggerTime)
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

                // Ensure the direction vector is not zero to avoid errors
                if (moveDirection != Vector3.zero)
                {
                    // Create a rotation that looks along the movement direction
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            transform.position = newPosition;


            // Check if it's time to change direction
            if (Time.time >= nextDirectionChangeTime)
            {
                // Change direction
                ChangeDirection();

                // Set the next time for direction change
                nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionInterval, maxChangeDirectionInterval);

                // Ensure the direction vector is not zero to avoid errors
                if (moveDirection != Vector3.zero)
                {
                    // Create a rotation that looks along the movement direction
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
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
        Vector3 randomDirection = Random.onUnitSphere.normalized;

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

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        //Fire();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Fire();
    }
}