using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Look Settings")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upperLookLimit = -80f;
    [SerializeField] private float lowerLookLimit = 80f;

    [Header("Outside Footstep Sounds (Grass/Leaves)")]
    [SerializeField] private AudioClip[] outsideWalkSounds;
    [SerializeField] private AudioClip[] outsideRunSounds;

    [Header("Inside Footstep Sounds (Concrete/Wood)")]
    [SerializeField] [UnityEngine.Serialization.FormerlySerializedAs("walkSounds")] private AudioClip[] insideWalkSounds;
    [SerializeField] [UnityEngine.Serialization.FormerlySerializedAs("runSounds")] private AudioClip[] insideRunSounds;

    [Header("Timing Settings")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;

    [Header("Head Bobbing")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobSpeed = 12f;
    [SerializeField] private float runBobAmount = 0.08f;
    [SerializeField] private float bobSmoothing = 8f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation = 0f;
    private bool isSprinting = false;

    private AudioSource footstepSource;
    private float stepTimer = 0f;

    // Head bob variables
    private Vector3 cameraStartLocalPos;
    private float bobTimer = 0f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>().transform;

        // Store the camera's original local position for head bob reference
        cameraStartLocalPos = playerCamera.localPosition;

        footstepSource = GetComponent<AudioSource>();
        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        footstepSource.loop = false;
        footstepSource.playOnAwake = false;
        footstepSource.volume = 0.5f;
    }

    public bool isMovementEnabled = true;

    void Update()
    {
        if (!isMovementEnabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            HandleFootsteps(); // Run footsteps check to ensure audio stops
            return;
        }

        HandleMouseLook();
        HandleMovement();
        HandleFootsteps();
        HandleHeadBob();
    }

    // 1. INPUT CALLBACKS
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    // 2. MOVEMENT
    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (Keyboard.current != null)
            isSprinting = Keyboard.current.leftShiftKey.isPressed;

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // 3. MOUSE LOOK
    private void HandleMouseLook()
    {
        if (playerCamera == null) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, upperLookLimit, lowerLookLimit);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // 4. FOOTSTEPS
    private void HandleFootsteps()
    {
        bool isMoving = moveInput.magnitude > 0.1f && isGrounded;

        if (!isMoving)
        {
            stepTimer = 0f;
            if (footstepSource.isPlaying)
                footstepSource.Stop();
            return;
        }

        stepTimer += Time.deltaTime;
        float interval = isSprinting ? runStepInterval : walkStepInterval;

        if (stepTimer >= interval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        bool isInside = PlayerPrefs.GetInt("HouseEntered", 0) == 1;
        AudioClip[] clips = null;

        if (isInside)
        {
            clips = isSprinting ? insideRunSounds : insideWalkSounds;
        }
        else
        {
            clips = isSprinting ? outsideRunSounds : outsideWalkSounds;
        }

        if (clips == null || clips.Length == 0) return;

        if (footstepSource.isPlaying)
            footstepSource.Stop();

        int randomIndex = Random.Range(0, clips.Length);
        footstepSource.clip = clips[randomIndex];
        footstepSource.Play();
    }

    // 5. HEAD BOBBING
    private void HandleHeadBob()
    {
        if (!enableHeadBob || playerCamera == null) return;

        bool isMoving = moveInput.magnitude > 0.1f && isGrounded;

        Vector3 targetPos;

        if (isMoving)
        {
            float bobSpeed = isSprinting ? runBobSpeed : walkBobSpeed;
            float bobAmount = isSprinting ? runBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * bobSpeed;

            // Vertical bob (up/down) using sine wave
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;

            // Slight horizontal sway (side to side) using cosine for natural offset
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;

            targetPos = cameraStartLocalPos + new Vector3(bobOffsetX, bobOffsetY, 0f);
        }
        else
        {
            // Reset timer slowly and return to neutral position when standing still
            bobTimer = 0f;
            targetPos = cameraStartLocalPos;
        }

        // Smoothly interpolate to avoid jitter
        playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetPos, Time.deltaTime * bobSmoothing);
    }
}