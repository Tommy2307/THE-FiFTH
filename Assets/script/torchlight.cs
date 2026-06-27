using UnityEngine;
using UnityEngine.InputSystem; 

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Component")]
    [SerializeField] private Light torchLight; 

    [Header("Audio Settings")]
    [SerializeField] private AudioClip switchSound; 
    private AudioSource audioSource;

    [Header("Flashlight Lag Settings")]
    [SerializeField] private bool useLag = true;
    [SerializeField] private float lagSpeed = 10f;

    private bool isOn = false;
    private Quaternion rotOffset;
    private Quaternion currentWorldRotation;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (torchLight != null)
        {
            torchLight.enabled = false;
        }

        // Store initial local rotation offset and world rotation
        rotOffset = transform.localRotation;
        currentWorldRotation = transform.rotation;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleTorch();
        }
    }

    void LateUpdate()
    {
        if (transform.parent != null)
        {
            // Target world rotation is parent's rotation combined with initial offset
            Quaternion targetWorldRot = transform.parent.rotation * rotOffset;

            if (useLag)
            {
                // Smoothly Slerp towards the target rotation
                currentWorldRotation = Quaternion.Slerp(currentWorldRotation, targetWorldRot, Time.deltaTime * lagSpeed);
                transform.rotation = currentWorldRotation;
            }
            else
            {
                transform.rotation = targetWorldRot;
                currentWorldRotation = targetWorldRot;
            }
        }
    }

    private void ToggleTorch()
    {
        if (torchLight == null) return;

        isOn = !isOn;
        torchLight.enabled = isOn;

        // Check if everything is linked properly
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
            Debug.Log("The sound code ran successfully!"); 
        }
        else
        {
            if (audioSource == null) Debug.LogError("Missing Audio Source Component on this object!");
            if (switchSound == null) Debug.LogError("You forgot to drag your sound file into the Switch Sound slot!");
        }
    }
}