using UnityEngine;

public class AmbientSound : MonoBehaviour
{
    [Header("Ambient Sounds")]
    [SerializeField] private AudioClip[] ambientClips;

    [Header("Settings")]
    [SerializeField] private float minDelay = 20f;
    [SerializeField] private float maxDelay = 60f;

    [Header("Wind Ambient Settings")]
    [SerializeField] private AudioClip windClip;
    [SerializeField] private float windVolume = 0.5f;
    [SerializeField] private float fadeSpeed = 1.0f;

    private AudioSource audioSource;
    private AudioSource windSource;
    private float timer = 0f;
    private float nextPlayTime;
    private int lastPlayedIndex = -1;
    private bool isPlaying = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false; // ← NEVER loop

        // Setup wind audio source
        windSource = gameObject.AddComponent<AudioSource>();
        windSource.clip = windClip;
        windSource.loop = true;
        windSource.playOnAwake = false;

        // Start playing wind sound if the player hasn't entered the house yet
        if (PlayerPrefs.GetInt("HouseEntered", 0) == 0 && windClip != null)
        {
            windSource.volume = windVolume;
            windSource.Play();
        }
        else
        {
            windSource.volume = 0f;
        }

        nextPlayTime = Random.Range(minDelay, maxDelay);
    }

    void Update()
    {
        // Handle wind volume fading
        if (windSource != null)
        {
            bool isInside = PlayerPrefs.GetInt("HouseEntered", 0) == 1;
            float targetVolume = isInside ? 0f : windVolume;
            
            if (!Mathf.Approximately(windSource.volume, targetVolume))
            {
                windSource.volume = Mathf.MoveTowards(windSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
                
                // If it faded to 0, stop playing to save resources
                if (windSource.volume <= 0f && windSource.isPlaying)
                {
                    windSource.Stop();
                }
                // If it needs to fade back in and is not playing, play it
                else if (windSource.volume > 0f && !windSource.isPlaying)
                {
                    windSource.Play();
                }
            }
        }

        // Wait for current sound to finish first
        if (isPlaying)
        {
            if (!audioSource.isPlaying)
            {
                // Sound finished — start countdown for next sound
                isPlaying = false;
                timer = 0f;
                nextPlayTime = Random.Range(minDelay, maxDelay);
            }
            return; // Don't count timer while sound is playing
        }

        // Count timer only when no sound is playing
        timer += Time.deltaTime;

        if (timer >= nextPlayTime)
        {
            PlayRandomSound();
        }
    }

    private void PlayRandomSound()
    {
        if (ambientClips == null || ambientClips.Length == 0) return;

        int randomIndex;
        do {
            randomIndex = Random.Range(0, ambientClips.Length);
        } while (randomIndex == lastPlayedIndex && ambientClips.Length > 1);

        lastPlayedIndex = randomIndex;
        AudioClip clip = ambientClips[randomIndex];

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            isPlaying = true; // Mark as playing
            timer = 0f;
        }
    }
}