using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Simple evidence manager that displays dialogue subtitles, plays voiceover clips,
/// and locks player controls during inspection. Delegates notebook updates to CaseFileManager.
/// </summary>
public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    [Header("UI Reference Elements (Will Auto-Find if left empty)")]
    [Tooltip("The text component used to show subtitles.")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    
    [Tooltip("The background panel wrapper containing the subtitle text.")]
    [SerializeField] private GameObject subtitleBg;

    [Header("Dialogue Timing Settings")]
    [Tooltip("Typing speed delay between characters in seconds.")]
    [SerializeField] private float typewriterSpeed = 0.025f;

    [Tooltip("Default delay in seconds to show a subtitle line after typing is done (if no audio clip is playing).")]
    [SerializeField] private float dialogueWaitTime = 2.0f;

    private AudioSource audioSource;
    private bool isInspecting = false;
    private EvidenceObject currentEvidence;
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private string activeLine = "";
    private Coroutine dialogueCoroutine;

    // Public getter
    public bool IsInspecting => isInspecting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Try to automatically find references if they are not assigned in the Inspector
        if (subtitleText == null)
        {
            subtitleText = FindComponentInScene<TextMeshProUGUI>("DialogueText");
            if (subtitleText == null) subtitleText = FindComponentInScene<TextMeshProUGUI>("SubtitleText");
        }

        if (subtitleBg == null)
        {
            subtitleBg = FindGameObjectInScene("DialogueBg");
            if (subtitleBg == null) subtitleBg = FindGameObjectInScene("SubtitleBg");
            
            // Fallback to text's own gameObject if background not found
            if (subtitleBg == null && subtitleText != null)
            {
                subtitleBg = subtitleText.gameObject;
            }
        }

        // Hide subtitle components initially
        if (subtitleBg != null) subtitleBg.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Starts the inspection sequence, locking player movements and playing dialogue automatically.
    /// </summary>
    public void StartInspection(EvidenceObject evidence)
    {
        if (isInspecting) return;

        isInspecting = true;
        currentEvidence = evidence;
        currentDialogueIndex = 0;

        // 1. Lock Player Movement and input
        TogglePlayerControls(false);

        // 2. Open subtitle display
        if (subtitleBg != null) subtitleBg.SetActive(true);

        // 3. Delegate to CaseFileManager to record and append evidence to the notebook
        CaseFileManager caseFile = FindFirstObjectByType<CaseFileManager>();
        if (caseFile != null)
        {
            caseFile.AddEvidence(evidence.EvidenceName, evidence.Observation);
        }
        else
        {
            Debug.LogWarning("[EvidenceManager] CaseFileManager not found in scene. Cannot append to notebook.");
        }

        // 4. Start automatic dialogue sequence coroutine
        if (evidence.DialogueLines != null && evidence.DialogueLines.Length > 0)
        {
            if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = StartCoroutine(PlayDialogueSequenceCoroutine());
        }
        else
        {
            EndInspection();
        }
    }

    private IEnumerator PlayDialogueSequenceCoroutine()
    {
        while (currentEvidence != null && currentDialogueIndex < currentEvidence.DialogueLines.Length)
        {
            activeLine = currentEvidence.DialogueLines[currentDialogueIndex];
            float voiceDuration = 0f;

            // Play voice clip if available and record its duration
            if (audioSource != null && currentEvidence.DialogueVoices != null && currentDialogueIndex < currentEvidence.DialogueVoices.Length)
            {
                AudioClip voiceClip = currentEvidence.DialogueVoices[currentDialogueIndex];
                if (voiceClip != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(voiceClip);
                    voiceDuration = voiceClip.length;
                }
            }

            // Start character typewriter print
            float startTime = Time.time;
            yield return StartCoroutine(TypeText(activeLine));

            // Wait for the remainder of the clip or default delay (whichever is longer)
            float totalWait = Mathf.Max(voiceDuration, dialogueWaitTime);
            
            while (Time.time - startTime < totalWait)
            {
                yield return null;
            }

            currentDialogueIndex++;
        }

        EndInspection();
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        subtitleText.text = "";

        foreach (char c in text)
        {
            subtitleText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
    }

    private void EndInspection()
    {
        isInspecting = false;

        if (subtitleBg != null) subtitleBg.SetActive(false);
        if (subtitleText != null) subtitleText.text = "";

        // Mark as inspected
        if (currentEvidence != null)
        {
            currentEvidence.MarkAsInspected();
        }

        // Unlock player controls
        TogglePlayerControls(true);

        currentEvidence = null;
        dialogueCoroutine = null;
    }

    private void TogglePlayerControls(bool enable)
    {
        // 1. FPS Movement Toggle
        FPSMovement movement = FindFirstObjectByType<FPSMovement>();
        if (movement != null)
        {
            movement.isMovementEnabled = enable;
        }

        // 2. Interaction Toggles
        PlayerInteraction interaction = FindFirstObjectByType<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.enabled = enable;
        }

        // 3. Cursor Locking updates
        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Helper utilities to find inactive objects in scene
    private T FindComponentInScene<T>(string name) where T : Component
    {
        T[] comps = Resources.FindObjectsOfTypeAll<T>();
        foreach (var c in comps)
        {
            if (c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(c.gameObject.scene.name))
            {
                return c;
            }
        }
        return null;
    }

    private GameObject FindGameObjectInScene(string name)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in objs)
        {
            if (obj.name.Equals(name, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(obj.scene.name))
            {
                return obj;
            }
        }
        return null;
    }
}
