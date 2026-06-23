using UnityEngine;
using TMPro;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ObjectiveManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ObjectiveManager");
                    instance = obj.AddComponent<ObjectiveManager>();
                }
            }
            return instance;
        }
    }
    private static ObjectiveManager instance;

    [Header("Current Objective")]
    [SerializeField] private string currentObjective = "Investigate the Area";

    [Header("Dialogue Timing Settings")]
    [SerializeField] private float typewriterSpeed = 0.025f; // Delay in seconds between characters (smaller is faster!)
    [SerializeField] private float dialogueWaitTime = 1.5f;   // Delay in seconds before the next line of dialogue starts (smaller is faster!)

    [Header("Dialogue UI References")]
    [SerializeField] private TextMeshProUGUI dialogueText; // Drag your custom dialogue Text component here!
    [SerializeField] private GameObject dialogueBg;        // Drag your dialogue background panel here (optional)
    [SerializeField] private GameObject idCardPanel;       // Drag your inspect ID Card image/panel here (optional)

    [Header("Dialogue Voice Settings")]
    [SerializeField] private AudioSource voiceSource;            // AudioSource used to play the dialogue voice lines
    [SerializeField] private AudioClip[] inspectDialogueVoices;  // Voice lines matching 1-to-1 with the ID Card Inspect dialogue lines
    [SerializeField] private AudioClip[] jammedDialogueVoices;   // Voice lines matching 1-to-1 with the Jammed Door dialogue lines

    public bool IsInspecting { get; private set; }

    private TextMeshProUGUI objectiveTextComponent;
    private Coroutine objectiveFadeCoroutine;
    private FPSMovement playerMovement;

    private string currentLineText = "";
    private bool isTyping = false;

    [Header("ID Card Inspect Dialogue")]
    [SerializeField] private string[] inspectDialogueLines = new string[]
    {
        "Rani's ID card...",
        "It's confirmed.",
        "She was here.",
        "Let's check the house."
    };
    private int currentDialogueIndex = 0;
    private System.Action onDialogueComplete;

    [Header("Jammed Door Dialogue")]
    [SerializeField] private string[] jammedDialogueLines = new string[]
    {
        "The door is jammed...",
        "I can't get out this way.",
        "I need to find another way out."
    };
    private int currentJammedIndex = 0;
    private bool isPlayingJammedDialogue = false;

    private void Awake()
    {
#if UNITY_EDITOR
        // Reset progress in Editor so hitting Play starts fresh every time
        PlayerPrefs.DeleteKey("ObjectiveState");
        PlayerPrefs.DeleteKey("HouseEntered");
        PlayerPrefs.Save();
#endif

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Load progress
        int state = PlayerPrefs.GetInt("ObjectiveState", 0);
        if (state == 0)
        {
            currentObjective = "Investigate the Area";
        }
        else if (state == 1)
        {
            currentObjective = "Investigate the House";
            DisableIDCardInScene();
        }
        else if (state == 2)
        {
            currentObjective = "Escape the House";
            DisableIDCardInScene();
        }
    }

    private void Start()
    {
        CreateUIElements();
        
        playerMovement = FindFirstObjectByType<FPSMovement>();

        // Automatically setup voiceSource if not assigned
        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Register any existing ObjectiveText in the scene
        ObjectiveUI objUI = FindFirstObjectByType<ObjectiveUI>();
        if (objUI != null)
        {
            TextMeshProUGUI textComp = objUI.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                RegisterObjectiveText(textComp);
            }
        }
    }

    private void CreateUIElements()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ObjectiveManager] No Canvas found in the scene!");
            return;
        }

        // Try to automatically find references if they were not assigned in the Inspector
        if (dialogueText == null)
        {
            dialogueText = FindComponentInScene<TextMeshProUGUI>("DialogueText");
            if (dialogueText == null) dialogueText = FindComponentInScene<TextMeshProUGUI>("SubtitleText");
        }

        if (dialogueBg == null)
        {
            dialogueBg = FindGameObjectInScene("DialogueBg");
            if (dialogueBg == null) dialogueBg = FindGameObjectInScene("SubtitleBg");
        }

        if (idCardPanel == null)
        {
            idCardPanel = FindGameObjectInScene("IDCardPanel");
        }

        // 1. Setup Dialogue UI
        if (dialogueText == null)
        {
            // Fallback: Create Dialogue Background & Text dynamically
            if (dialogueBg == null)
            {
                dialogueBg = new GameObject("DialogueBg");
                dialogueBg.transform.SetParent(canvas.transform, false);
                UnityEngine.UI.Image bgImg = dialogueBg.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0f); // Transparent
                
                RectTransform bgRect = dialogueBg.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0f, 0.05f);
                bgRect.anchorMax = new Vector2(1f, 0.22f);
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }

            GameObject dialogueObj = new GameObject("DialogueText");
            dialogueObj.transform.SetParent(dialogueBg.transform, false);
            
            dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 24;
            dialogueText.alignment = TextAlignmentOptions.Center;
            dialogueText.color = Color.white;
            dialogueText.outlineWidth = 0.2f;
            dialogueText.outlineColor = Color.black;

            RectTransform textRect = dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(50, 10);
            textRect.offsetMax = new Vector2(-50, -10);
        }
        else
        {
            // If the user assigned dialogueText in the inspector but dialogueBg is null,
            // let dialogueText's gameObject act as the parent wrapper.
            if (dialogueBg == null)
            {
                dialogueBg = dialogueText.gameObject;
            }
        }

        if (dialogueBg != null) dialogueBg.SetActive(false);

        // 2. Setup ID Card UI
        if (idCardPanel == null)
        {
            idCardPanel = new GameObject("IDCardPanel");
            idCardPanel.transform.SetParent(canvas.transform, false);
            
            UnityEngine.UI.Image cardImage = idCardPanel.AddComponent<UnityEngine.UI.Image>();
            cardImage.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Fallback color

            RectTransform cardRect = idCardPanel.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(400, 250); // Default fallback size

            // Load ID Card texture from assets
            string path = System.IO.Path.Combine(Application.dataPath, "Models/id_card_model/textures/id card.png");
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.Combine(Application.dataPath, "Models/id_card_model/textures/Employee_Id_Card_Design_baseColor.png");
            }

            if (System.IO.File.Exists(path))
            {
                try
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileData))
                    {
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        cardImage.sprite = sprite;
                        cardImage.color = Color.white;

                        // Calculate correct size based on aspect ratio (target height is 300)
                        float aspect = (float)tex.width / tex.height;
                        cardRect.sizeDelta = new Vector2(300f * aspect, 300f);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[ObjectiveManager] Failed to load ID Card texture: " + e.Message);
                }
            }
            else
            {
                Debug.LogWarning("[ObjectiveManager] ID Card texture file not found at: " + path);
                
                // Fallback design if file is missing (simple stylized box)
                cardImage.color = new Color(0.08f, 0.08f, 0.12f, 1f);
                
                GameObject textObj = new GameObject("FallbackText");
                textObj.transform.SetParent(idCardPanel.transform, false);
                TextMeshProUGUI fallbackText = textObj.AddComponent<TextMeshProUGUI>();
                fallbackText.fontSize = 20;
                fallbackText.alignment = TextAlignmentOptions.Center;
                fallbackText.color = Color.white;
                fallbackText.text = "<b>ID CARD</b>\n\nNAME: Rani\nSTATUS: Missing";
                
                RectTransform fallbackRect = textObj.GetComponent<RectTransform>();
                fallbackRect.anchorMin = Vector2.zero;
                fallbackRect.anchorMax = Vector2.one;
                fallbackRect.sizeDelta = Vector2.zero;
            }
        }

        idCardPanel.SetActive(false);
    }

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

    private void DisableIDCardInScene()
    {
        IDCardPickup idCard = FindFirstObjectByType<IDCardPickup>();
        if (idCard != null)
        {
            Destroy(idCard.gameObject);
        }
    }

    public void RegisterObjectiveText(TextMeshProUGUI textComp)
    {
        objectiveTextComponent = textComp;
        if (objectiveTextComponent != null)
        {
            objectiveTextComponent.text = "Objective: " + currentObjective;
            
            Color color = objectiveTextComponent.color;
            objectiveTextComponent.color = new Color(color.r, color.g, color.b, 1f);
            objectiveTextComponent.gameObject.SetActive(true);

            if (objectiveFadeCoroutine != null) StopCoroutine(objectiveFadeCoroutine);
            objectiveFadeCoroutine = StartCoroutine(DisplayAndFadeObjective());
        }
    }

    public void UpdateObjective(string newObjective)
    {
        if (currentObjective == newObjective) return;

        currentObjective = newObjective;
        
        int state = 0;
        if (newObjective == "Investigate the House") state = 1;
        else if (newObjective == "Escape the House") state = 2;

        PlayerPrefs.SetInt("ObjectiveState", state);
        PlayerPrefs.Save();

        if (objectiveTextComponent != null)
        {
            if (objectiveFadeCoroutine != null) StopCoroutine(objectiveFadeCoroutine);
            objectiveFadeCoroutine = StartCoroutine(ChangeAndFadeObjectiveCoroutine("Objective: " + newObjective));
        }
    }

    private IEnumerator ChangeAndFadeObjectiveCoroutine(string newText)
    {
        float duration = 1.0f;
        float elapsed = 0f;
        Color color = objectiveTextComponent.color;
        float startAlpha = color.a;

        // Fade out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            objectiveTextComponent.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        objectiveTextComponent.color = new Color(color.r, color.g, color.b, 0f);

        objectiveTextComponent.text = newText;
        objectiveTextComponent.gameObject.SetActive(true);

        // Fade in
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            objectiveTextComponent.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        objectiveTextComponent.color = new Color(color.r, color.g, color.b, 1f);

        yield return StartCoroutine(DisplayAndFadeObjective());
    }

    private IEnumerator DisplayAndFadeObjective()
    {
        yield return new WaitForSeconds(8f);

        float duration = 2.0f;
        float elapsed = 0f;
        Color color = objectiveTextComponent.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            objectiveTextComponent.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        objectiveTextComponent.color = new Color(color.r, color.g, color.b, 0f);
        objectiveTextComponent.gameObject.SetActive(false);
    }

    // ── ID Card Inspection dialogue ───────────────────────────

    public void StartInspectDialogue(System.Action onComplete)
    {
        if (IsInspecting) return;
        
        IsInspecting = true;
        onDialogueComplete = onComplete;
        
        if (idCardPanel != null) idCardPanel.SetActive(true);
        if (dialogueBg != null) dialogueBg.SetActive(true);
        
        if (playerMovement == null) playerMovement = FindFirstObjectByType<FPSMovement>();
        if (playerMovement != null) playerMovement.isMovementEnabled = false;

        StartCoroutine(PlayInspectDialogueCoroutine());
    }

    private IEnumerator PlayInspectDialogueCoroutine()
    {
        currentDialogueIndex = 0;
        while (currentDialogueIndex < inspectDialogueLines.Length)
        {
            currentLineText = inspectDialogueLines[currentDialogueIndex];

            // Play corresponding voice clip if assigned
            if (voiceSource != null && inspectDialogueVoices != null && currentDialogueIndex < inspectDialogueVoices.Length)
            {
                AudioClip voiceClip = inspectDialogueVoices[currentDialogueIndex];
                if (voiceClip != null)
                {
                    voiceSource.Stop(); // Stop any currently playing voice line
                    voiceSource.PlayOneShot(voiceClip);
                }
            }

            currentDialogueIndex++;
            
            yield return StartCoroutine(TypeText(currentLineText));
            
            // Wait before typing the next line automatically
            yield return new WaitForSeconds(dialogueWaitTime);
        }
        
        // Finished Dialogue
        if (idCardPanel != null) idCardPanel.SetActive(false);
        if (dialogueBg != null) dialogueBg.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        
        IsInspecting = false;
        
        if (playerMovement != null) playerMovement.isMovementEnabled = true;

        UpdateObjective("Investigate the House");

        onDialogueComplete?.Invoke();
    }

    public void AdvanceInspectDialogue()
    {
        // Purely automatic dialogue, E press does not skip or advance
    }

    // ── Jammed Door dialogue ───────────────────────────

    public void TriggerJammedDialogue()
    {
        if (isPlayingJammedDialogue || PlayerPrefs.GetInt("HouseEntered", 0) == 1) return;
        
        PlayerPrefs.SetInt("HouseEntered", 1);
        PlayerPrefs.Save();
        
        isPlayingJammedDialogue = true;
        currentJammedIndex = 0;
        
        UpdateObjective("Escape the House");
        
        if (dialogueBg != null) dialogueBg.SetActive(true);
        StartCoroutine(PlayJammedDialogueCoroutine());
    }

    private IEnumerator PlayJammedDialogueCoroutine()
    {
        while (currentJammedIndex < jammedDialogueLines.Length)
        {
            currentLineText = jammedDialogueLines[currentJammedIndex];

            // Play corresponding voice clip if assigned
            if (voiceSource != null && jammedDialogueVoices != null && currentJammedIndex < jammedDialogueVoices.Length)
            {
                AudioClip voiceClip = jammedDialogueVoices[currentJammedIndex];
                if (voiceClip != null)
                {
                    voiceSource.Stop(); // Stop any currently playing voice line
                    voiceSource.PlayOneShot(voiceClip);
                }
            }

            currentJammedIndex++;
            
            yield return StartCoroutine(TypeText(currentLineText));
            
            yield return new WaitForSeconds(dialogueWaitTime);
        }
        
        if (dialogueText != null) dialogueText.text = "";
        if (dialogueBg != null) dialogueBg.SetActive(false);
        isPlayingJammedDialogue = false;
    }

    // ── Typewriter Core ─────────────────────────────────────

    private IEnumerator TypeText(string text)
    {
        if (dialogueText == null) yield break;
        
        dialogueText.text = "";
        isTyping = true;
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed); 
        }
        
        isTyping = false;
    }
}
