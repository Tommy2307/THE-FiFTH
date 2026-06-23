using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3.5f;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI uiText;

    [Header("Hand Items Visuals")]
    [SerializeField] private GameObject handCrowbar; // Drag your hand-attached crowbar here!
    [SerializeField] private GameObject handIDCard;  // Drag your hand-attached ID card here!

    private Camera cam;
    private PlayerInventory inventory;
    private string currentLockMessage = "";
    private float messageTimer = 0f;

    void Start()
    {
        cam = Camera.main;

        if (uiText == null)
        {
            GameObject uiObj = GameObject.Find("intraction");
            if (uiObj != null)
                uiText = uiObj.GetComponent<TextMeshProUGUI>();
            else
                uiText = FindFirstObjectByType<TextMeshProUGUI>();
        }

        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        // Try to automatically find references if null
        if (handCrowbar == null)
        {
            handCrowbar = FindChildByName("Hand Crowbar");
        }
        if (handIDCard == null)
        {
            handIDCard = FindChildByName("Hand ID Card");
        }

        // Ensure hand visuals start invisible on boot
        if (handCrowbar != null) handCrowbar.SetActive(false);
        if (handIDCard != null) handIDCard.SetActive(false);
    }

    private GameObject FindChildByName(string childName)
    {
        // 1. Search in this GameObject and all its children (including inactive)
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }
        
        // 2. Search in parent and parent's children (including inactive)
        Transform current = transform.parent;
        while (current != null)
        {
            Transform[] parentChildren = current.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in parentChildren)
            {
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }
            current = current.parent;
        }

        // 3. Search globally (finds inactive scene objects too)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(obj.scene.name))
            {
                return obj;
            }
        }

        return null;
    }

    // ── Public functions to manage hand visual states ──────────────────

    public void ShowCrowbarInHand()
    {
        if (handCrowbar != null) handCrowbar.SetActive(true);
    }

    public void HideCrowbarInHand()
    {
        if (handCrowbar != null) handCrowbar.SetActive(false);
    }

    public void ShowIDCardInHand()
    {
        if (handIDCard != null) handIDCard.SetActive(true);
    }

    public void HideIDCardInHand()
    {
        if (handIDCard != null) handIDCard.SetActive(false);
    }

    // ── Main Update Raycast Logic ─────────────────────────────────────

    void Update()
    {
        bool inputPressedThisFrame = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        
        // Inspect dialogue input bypass
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsInspecting)
        {
            if (inputPressedThisFrame)
            {
                ObjectiveManager.Instance.AdvanceInspectDialogue();
            }
            if (uiText != null) uiText.text = "";
            return;
        }

        bool inputConsumedThisFrame = false; 

        Letter[] allLetters = FindObjectsByType<Letter>(FindObjectsSortMode.None);
        foreach (Letter l in allLetters)
        {
            if (l.IsReading)
            {
                if (uiText != null) uiText.text = l.GetPrompt();

                if (inputPressedThisFrame)
                {
                    l.CloseLetter();
                    inputConsumedThisFrame = true;
                }

                return; 
            }
        }

        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (uiText != null) uiText.text = currentLockMessage;
            if (messageTimer <= 0) currentLockMessage = "";
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // 1. KEY
            if (hit.collider.CompareTag("Key"))
            {
                if (uiText != null) uiText.text = "Press E to pick up the key";

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    KeyItem keyScript = hit.collider.GetComponent<KeyItem>();
                    if (keyScript != null)
                    {
                        bool success = keyScript.PickUp(inventory);
                        if (!success)
                        {
                            currentLockMessage = "Your hand is full!";
                            messageTimer = 2.5f;
                        }
                    }
                }
                return;
            }

            // 1b. CROWBAR PICKUP
            CrowbarPickup crowbarScript = hit.collider.GetComponent<CrowbarPickup>()
                ?? hit.collider.GetComponentInParent<CrowbarPickup>();

            if (crowbarScript != null)
            {
                if (uiText != null) uiText.text = crowbarScript.GetPickupPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    crowbarScript.InteractWithCrowbar(gameObject);
                }
                return;
            }

            // 1c. ID CARD PICKUP
            IDCardPickup idCardScript = hit.collider.GetComponent<IDCardPickup>()
                ?? hit.collider.GetComponentInParent<IDCardPickup>();

            if (idCardScript != null)
            {
                if (uiText != null) uiText.text = idCardScript.GetPickupPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    idCardScript.InteractWithIDCard(gameObject);
                }
                return;
            }

            // 2. LETTER
            Letter letterScript = hit.collider.GetComponent<Letter>()
                ?? hit.collider.GetComponentInParent<Letter>();

            if (letterScript != null)
            {
                if (uiText != null) uiText.text = letterScript.GetPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    letterScript.OpenLetter();
                    return;
                }

                return;
            }

            // 3. RADIO
            RadioInteract radio = hit.collider.GetComponent<RadioInteract>()
                ?? hit.collider.GetComponentInParent<RadioInteract>();

            if (radio != null)
            {
                if (uiText != null) uiText.text = radio.GetPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                    radio.Interact();

                return;
            }

            // 4. DOOR
            Door doorScript = hit.collider.GetComponent<Door>()
                ?? hit.collider.GetComponentInParent<Door>()
                ?? hit.collider.GetComponentInChildren<Door>();

            if (doorScript != null)
            {
                if (uiText != null) uiText.text = doorScript.GetDoorPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    string returnedMessage = "";
                    doorScript.InteractWithDoor(ref returnedMessage);

                    if (!string.IsNullOrEmpty(returnedMessage))
                    {
                        currentLockMessage = returnedMessage;
                        messageTimer = 2.5f;
                    }
                }
                return;
            }
        }

        if (uiText != null && string.IsNullOrEmpty(currentLockMessage))
            uiText.text = "";
    }
}