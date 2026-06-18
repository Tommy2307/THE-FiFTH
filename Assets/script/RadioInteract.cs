using UnityEngine;

public class RadioInteract : MonoBehaviour
{
    [Header("Radio Audio")]
    [SerializeField] private AudioClip radioSound;
    [SerializeField] private float volume = 1f;

    [Header("Radio On/Off Sounds")]
    [SerializeField] private AudioClip radioOnSound;
    [SerializeField] private AudioClip radioOffSound;

    private AudioSource audioSource;
    private AudioSource sfxSource; // separate source for on/off sounds
    private bool isPlaying = false;
    private float lastInteractTime = 0f;
    private float interactCooldown = 0.5f;

    void Start()
    {
        // Main radio music source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = radioSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        // Separate source for on/off click sounds
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f;
    }

    public void Interact()
    {
        if (Time.time - lastInteractTime < interactCooldown) return;
        lastInteractTime = Time.time;

        if (!isPlaying)
        {
            // Play on sound first then radio
            if (radioOnSound != null)
                sfxSource.PlayOneShot(radioOnSound);

            audioSource.Play();
            isPlaying = true;
            Debug.Log("[RADIO] Playing!");
        }
        else
        {
            // Play off sound then stop radio
            if (radioOffSound != null)
                sfxSource.PlayOneShot(radioOffSound);

            audioSource.Stop();
            isPlaying = false;
            Debug.Log("[RADIO] Stopped!");
        }
    }

    public string GetPrompt()
    {
        return isPlaying ? "Press E to Turn Off Radio" : "Press E to Turn On Radio";
    }
}