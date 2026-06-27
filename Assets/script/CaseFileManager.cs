using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class CaseFileManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject caseFilePanel;

    [Header("Page Settings")]
    [Tooltip("The main page GameObject representing the Case File.")]
    [SerializeField] private GameObject caseFilePage;
    
    [Tooltip("The secondary page GameObject representing the Evidence observations.")]
    [SerializeField] private GameObject evidencePage;

    [Header("Evidence Integration Settings")]
    [Tooltip("The TextMeshPro text component on the second page (Left side) where evidence entries are written.")]
    [SerializeField] private TextMeshProUGUI notebookEvidenceText;

    [Tooltip("The TextMeshPro text component on the second page (Right side) where overflowing evidence entries are written.")]
    [SerializeField] private TextMeshProUGUI rightPageEvidenceText;

    [Header("Animation")]
    [SerializeField] private Animator caseFileAnimator;
    [SerializeField] private float closeAnimationDuration = 0.10f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [Tooltip("Sound played when flipping pages by pressing Q.")]
    [SerializeField] private AudioClip turnPageSound;

    private AudioSource audioSource;
    private bool isCaseFileOpen = false;
    private int currentPageIndex = 0;

    // Track inspected evidence names persistently
    private List<string> collectedEvidenceNames = new List<string>();

    void Awake()
    {
#if UNITY_EDITOR
        // Reset evidence list in Editor so hitting Play starts fresh every time
        PlayerPrefs.DeleteKey("CollectedEvidence");
        PlayerPrefs.Save();
#endif
    }

    void Start()
    {
        if (caseFilePanel != null)
        {
            caseFilePanel.SetActive(false);
        }

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Initialize pages state
        UpdateActivePage();

        // Load saved collected evidence from PlayerPrefs and update the UI
        LoadCollectedEvidence();
        UpdateNotebookText();
    }

    void Update()
    {
        if (isCaseFileOpen)
        {
            // Close notebook inputs
            if (Keyboard.current != null &&
                (Keyboard.current.tabKey.wasPressedThisFrame ||
                 Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ToggleCaseFile();
                return;
            }

            // Toggle pages input (Press Q to swap pages)
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                TogglePages();
            }

            return;
        }

        if (ObjectiveManager.Instance != null &&
            ObjectiveManager.Instance.IsInspecting)
            return;

        Letter[] allLetters = FindObjectsByType<Letter>(FindObjectsSortMode.None);

        foreach (Letter l in allLetters)
        {
            if (l.IsReading)
                return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleCaseFile();
        }
    }

    private void ToggleCaseFile()
    {
        if (caseFilePanel == null)
        {
            Debug.LogWarning("Case File Panel not assigned!");
            return;
        }

        if (!isCaseFileOpen)
        {
            // OPEN BOOK
            isCaseFileOpen = true;

            // Reset back to main case file page (Page 0) on open
            currentPageIndex = 0;
            UpdateActivePage();

            caseFilePanel.SetActive(true);

            if (caseFileAnimator != null)
                caseFileAnimator.SetTrigger("open");

            PauseGameForUI();

            if (openSound != null)
                audioSource.PlayOneShot(openSound);
        }
        else
        {
            // CLOSE BOOK
            isCaseFileOpen = false;

            if (caseFileAnimator != null)
                caseFileAnimator.SetTrigger("close");

            if (closeSound != null)
                audioSource.PlayOneShot(closeSound);

            ResumeGameFromUI();

            Invoke(nameof(HideCaseFile), closeAnimationDuration);
        }
    }

    /// <summary>
    /// Adds a new evidence item to the notebook, saves the list, and refreshes the text page.
    /// </summary>
    public void AddEvidence(string name, string observation)
    {
        if (!collectedEvidenceNames.Contains(name))
        {
            collectedEvidenceNames.Add(name);
            SaveCollectedEvidence();
            UpdateNotebookText();
        }
    }

    /// <summary>
    /// Formats and updates the text displayed on the Evidence sub-page.
    /// </summary>
    private void UpdateNotebookText()
    {
        if (notebookEvidenceText == null) return;

        if (collectedEvidenceNames.Count == 0)
        {
            notebookEvidenceText.text = "";
            if (rightPageEvidenceText != null)
            {
                rightPageEvidenceText.text = "";
            }
            return;
        }

        // 1. Format Left Page (First 3 items: indices 0, 1, 2)
        System.Text.StringBuilder leftSb = new System.Text.StringBuilder();
        int leftCount = Mathf.Min(collectedEvidenceNames.Count, 3);
        
        for (int i = 0; i < leftCount; i++)
        {
            string name = collectedEvidenceNames[i];
            string obs = GetObservationForEvidence(name);

            leftSb.AppendLine($"*{name}");
            leftSb.AppendLine(obs);

            if (i < leftCount - 1)
            {
                leftSb.AppendLine(); // Spacing between items
            }
        }
        notebookEvidenceText.text = leftSb.ToString();

        // 2. Format Right Page (Remaining items starting from index 3)
        if (rightPageEvidenceText != null)
        {
            if (collectedEvidenceNames.Count > 3)
            {
                System.Text.StringBuilder rightSb = new System.Text.StringBuilder();
                for (int i = 3; i < collectedEvidenceNames.Count; i++)
                {
                    string name = collectedEvidenceNames[i];
                    string obs = GetObservationForEvidence(name);

                    rightSb.AppendLine($"*{name}");
                    rightSb.AppendLine(obs);

                    if (i < collectedEvidenceNames.Count - 1)
                    {
                        rightSb.AppendLine(); // Spacing between items
                    }
                }
                rightPageEvidenceText.text = rightSb.ToString();
            }
            else
            {
                rightPageEvidenceText.text = "";
            }
        }
    }

    /// <summary>
    /// Dynamic lookup to fetch the Observation description from an EvidenceObject in the scene.
    /// </summary>
    private string GetObservationForEvidence(string name)
    {
        EvidenceObject[] allEvidences = Resources.FindObjectsOfTypeAll<EvidenceObject>();
        foreach (var ev in allEvidences)
        {
            if (ev.EvidenceName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return ev.Observation;
            }
        }
        return "No observation recorded.";
    }

    private void SaveCollectedEvidence()
    {
        string savedString = string.Join(";", collectedEvidenceNames);
        PlayerPrefs.SetString("CollectedEvidence", savedString);
        PlayerPrefs.Save();
    }

    private void LoadCollectedEvidence()
    {
        collectedEvidenceNames.Clear();
        string savedString = PlayerPrefs.GetString("CollectedEvidence", "");
        if (!string.IsNullOrEmpty(savedString))
        {
            collectedEvidenceNames = new List<string>(savedString.Split(';'));
        }
    }

    private void TogglePages()
    {
        currentPageIndex = (currentPageIndex + 1) % 2; // Swap between 0 and 1
        UpdateActivePage();

        if (turnPageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(turnPageSound);
        }
    }

    private void UpdateActivePage()
    {
        // Only toggle sub-pages if the notebook is currently open, preventing it from showing on boot
        if (!isCaseFileOpen) return;

        if (caseFilePage != null)
        {
            caseFilePage.SetActive(currentPageIndex == 0);
        }
        if (evidencePage != null)
        {
            evidencePage.SetActive(currentPageIndex == 1);
        }
    }

    private void HideCaseFile()
    {
        caseFilePanel.SetActive(false);

        // Explicitly disable both pages upon closing to ensure nothing is left rendering
        if (caseFilePage != null) caseFilePage.SetActive(false);
        if (evidencePage != null) evidencePage.SetActive(false);
    }

    private void PauseGameForUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerInteraction playerInteract = FindFirstObjectByType<PlayerInteraction>();

        if (playerInteract != null)
            playerInteract.enabled = false;

        FPSMovement movement = FindFirstObjectByType<FPSMovement>();

        if (movement != null)
            movement.isMovementEnabled = false;
    }

    private void ResumeGameFromUI()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerInteraction playerInteract = FindFirstObjectByType<PlayerInteraction>();

        if (playerInteract != null)
            playerInteract.enabled = true;

        FPSMovement movement = FindFirstObjectByType<FPSMovement>();

        if (movement != null)
            movement.isMovementEnabled = true;
    }
}