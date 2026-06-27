using UnityEngine;

/// <summary>
/// Place this script on any evidence object in the scene (e.g. Camera Bag, Torn Notes, Footprints).
/// Configures Virat's inspection dialogue and notebook observations directly in the Unity Inspector.
/// </summary>
public class EvidenceObject : MonoBehaviour
{
    [Header("Evidence Details")]
    [Tooltip("The name of this evidence object (used in the interaction prompt).")]
    [SerializeField] private string evidenceName = "Evidence";
    
    [Tooltip("If enabled, this evidence can only be inspected once.")]
    [SerializeField] private bool inspectOnlyOnce = true;

    [Header("Officer Dialogue Settings")]
    [Tooltip("Officer Virat's text dialogue subtitles when inspecting this object.")]
    [TextArea(2, 3)]
    [SerializeField] private string[] dialogueLines;

    [Tooltip("Officer Virat's voice clip files matching 1-to-1 with the dialogue lines.")]
    [SerializeField] private AudioClip[] dialogueVoices;

    [Header("Notebook Integration Settings")]
    [Tooltip("The short observation written down in the notebook.")]
    [TextArea(2, 4)]
    [SerializeField] private string observation = "A short observation about this evidence.";

    // Track inspected state
    private bool hasBeenInspected = false;

    // Public Getters
    public string EvidenceName => evidenceName;
    public bool InspectOnlyOnce => inspectOnlyOnce;
    public bool HasBeenInspected => hasBeenInspected;
    public string[] DialogueLines => dialogueLines;
    public AudioClip[] DialogueVoices => dialogueVoices;
    public string Observation => observation;

    /// <summary>
    /// Mark this evidence as inspected.
    /// </summary>
    public void MarkAsInspected()
    {
        hasBeenInspected = true;
    }

    /// <summary>
    /// Returns the prompt text to show when looking at the object.
    /// </summary>
    public string GetPrompt()
    {
        return "Press E to Inspect " + evidenceName;
    }

    /// <summary>
    /// Triggers the dialogue inspection.
    /// </summary>
    public void Inspect()
    {
        if (inspectOnlyOnce && hasBeenInspected) return;

        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.StartInspection(this);
        }
        else
        {
            Debug.LogError("[EvidenceObject] EvidenceManager is missing in the scene!");
        }
    }
}
