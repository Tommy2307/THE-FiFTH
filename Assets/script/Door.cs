using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private KeyType requiredKey = KeyType.Key1;
    [SerializeField] private bool isLocked = true;
    [SerializeField] private bool isMainDoor = false;

    [Header("Crowbar Door Settings")]
    [SerializeField] private bool isCrowbarDoor = false;      // Tick this for the main door
    [SerializeField] private float doorOpenDuration = 20f;    // How long before it slams shut (e.g. 15 to 30 seconds)
    [SerializeField] private float jamShakeIntensity = 0.05f;
    [SerializeField] private float jamShakeDuration = 0.5f;

    [Header("Door Sound Effects")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip crowbarPrySound;       // Pry metal sound
    [SerializeField] private AudioClip jamSound;              // Thud when jammed

    private PlayerInventory playerInventory;
    private Animator animator;
    private AudioSource audioSource;

    private bool isUnlocked = false;
    private bool isOpen = false;
    private bool hasInteractedOnce = false;
    private float lastInteractTime = 0f;
    private float interactCooldown = 0.2f;

    // Crowbar door state
    private enum CrowbarState { Closed, Opening, Open, Closing, Jammed }
    private CrowbarState crowbarState = CrowbarState.Closed;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (!isLocked) isUnlocked = true;
        isOpen = false;

        // Safety override: if the duration in the inspector is still set to the old 3 seconds default, bump it to 20 seconds
        if (doorOpenDuration < 2)
        {
            doorOpenDuration = 5f;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerInventory = player.GetComponent<PlayerInventory>();
    }

    // ── Prompt ────────────────────────────────────────────────────────

    public string GetDoorPrompt()
    {
        // Route to crowbar logic first so it doesn't get skipped
        if (isCrowbarDoor)
        {
            switch (crowbarState)
            {
                case CrowbarState.Closed:
                    if (playerInventory != null && playerInventory.HasCrowbar())
                    {
                        return "Press E to pry open";
                    }
                    return "Press E to open"; // Show this first before player knows it needs a crowbar

                case CrowbarState.Jammed:
                    return "Door is jammed";

                default:
                    return "";
            }
        }

        // Original door prompts
        if (isMainDoor)
            return "Main Door (Locked)";

        if (!isUnlocked)
            return hasInteractedOnce ? "Needs Key To open" : "Press E to Use the Key";
        else
            return isOpen ? "Press E to Close Door" : "Press E to Open Door";
    }

    // ── Interact ──────────────────────────────────────────────────────

    public void InteractWithDoor(ref string uiMessage)
    {
        if (Time.time - lastInteractTime < interactCooldown) return;
        lastInteractTime = Time.time;

        // Route to crowbar logic first
        if (isCrowbarDoor)
        {
            HandleCrowbarDoor(ref uiMessage);
            return;
        }

        // Original door logic (unchanged)
        if (isMainDoor)
        {
            uiMessage = "Door is Locked!";
            PlaySound(lockedSound);
            return;
        }

        if (!isUnlocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKey))
            {
                uiMessage = "";
                isUnlocked = true;
                hasInteractedOnce = false;
                playerInventory.UseKey(requiredKey);
                isOpen = true;

                PlaySound(openSound);

                if (animator != null)
                {
                    animator.ResetTrigger("close");
                    animator.SetTrigger("open");
                }
            }
            else
            {
                uiMessage = "Locked! You need Key.";
                hasInteractedOnce = true;
                PlaySound(lockedSound);
            }
        }
        else
        {
            isOpen = !isOpen;
            uiMessage = "";

            if (isOpen)
            {
                PlaySound(openSound);
                if (animator != null)
                {
                    animator.ResetTrigger("close");
                    animator.SetTrigger("open");
                }
            }
            else
            {
                PlaySound(closeSound);
                if (animator != null)
                {
                    animator.ResetTrigger("open");
                    animator.SetTrigger("close");
                }
            }
        }
    }

    // ── Crowbar Door Logic ────────────────────────────────────────────

    private void HandleCrowbarDoor(ref string uiMessage)
    {
        switch (crowbarState)
        {
            case CrowbarState.Closed:
                if (playerInventory != null && playerInventory.HasCrowbar())
                {
                    StartCoroutine(CrowbarOpenSequence());
                    uiMessage = "";
                }
                else
                {
                    uiMessage = "Need a crowbar to open this door";
                    PlaySound(lockedSound);
                }
                break;

            case CrowbarState.Jammed:
                uiMessage = "The door is completely jammed shut.";
                PlaySound(lockedSound);
                break;

            // Opening / Open / Closing — ignore input
            default:
                uiMessage = "";
                break;
        }
    }

    IEnumerator CrowbarOpenSequence()
    {
        crowbarState = CrowbarState.Opening;

        // Pry sound
        PlaySound(crowbarPrySound);
        yield return new WaitForSeconds(0.6f);

        // Creak and open
        PlaySound(openSound);
        if (animator != null)
        {
            animator.ResetTrigger("close");
            animator.SetTrigger("open");
        }

        // Consume the crowbar on use (make it disappear from inventory/hand)
        if (playerInventory != null)
        {
            playerInventory.RemoveCrowbar();
        }

        crowbarState = CrowbarState.Open;

        // Stay open then auto slam shut
        yield return new WaitForSeconds(doorOpenDuration);

        StartCoroutine(CloseAndJamSequence());
    }

    IEnumerator CloseAndJamSequence()
    {
        crowbarState = CrowbarState.Closing;

        PlaySound(closeSound);
        if (animator != null)
        {
            animator.ResetTrigger("open");
            animator.SetTrigger("close");
        }

        yield return new WaitForSeconds(1.2f); // Adjust this to match your close animation length

        PlaySound(jamSound);
        crowbarState = CrowbarState.Jammed;

        // Trigger dialogue and update objective
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.TriggerJammedDialogue();
        }

        StartCoroutine(ShakeDoor());
    }

    IEnumerator ShakeDoor()
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < jamShakeDuration)
        {
            float offsetX = Random.Range(-jamShakeIntensity, jamShakeIntensity);
            float offsetZ = Random.Range(-jamShakeIntensity, jamShakeIntensity);
            transform.localPosition = new Vector3(
                originalPos.x + offsetX,
                originalPos.y,
                originalPos.z + offsetZ
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}