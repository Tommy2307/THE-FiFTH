using UnityEngine;
using UnityEngine.InputSystem;

public class Letter : MonoBehaviour
{
    [Header("Letter UI")]
    [SerializeField] private GameObject letterUI;

    [Header("Letter Mesh")]
    [SerializeField] private Renderer letterMesh;

    [Header("Player Reference")]
    [SerializeField] private FPSMovement player;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip pickUpSound;
    [SerializeField] private AudioClip closeSound;

    private AudioSource audioSource;
    private bool isReading = false;
    private float lastToggleTime = -10f;
    private float toggleCooldown = 0.3f; // half a second guard

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<FPSMovement>();

        if (letterUI != null)
            letterUI.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void OpenLetter()
    {
        if (Time.unscaledTime - lastToggleTime < toggleCooldown) return; // BLOCK rapid re-trigger
        lastToggleTime = Time.unscaledTime;

        if (isReading) return; // already open, ignore

        Debug.Log("[LETTER] OpenLetter() called! Frame: " + Time.frameCount);

        isReading = true;

        if (pickUpSound != null)
            audioSource.PlayOneShot(pickUpSound);

        if (letterUI != null) letterUI.SetActive(true);
        if (letterMesh != null) letterMesh.enabled = false;
        if (player != null) player.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseLetter()
    {
        if (Time.unscaledTime - lastToggleTime < toggleCooldown) return; // BLOCK rapid re-trigger
        lastToggleTime = Time.unscaledTime;

        if (!isReading) return; // already closed, ignore

        Debug.Log("[LETTER] CloseLetter() called! Frame: " + Time.frameCount);

        isReading = false;

        if (closeSound != null)
            audioSource.PlayOneShot(closeSound);

        if (letterUI != null) letterUI.SetActive(false);
        if (letterMesh != null) letterMesh.enabled = true;
        if (player != null) player.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsReading => isReading;

    public string GetPrompt()
    {
        return isReading ? "Press E to put down" : "Press E to Read";
    }
}