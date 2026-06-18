using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3.5f;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI uiText;

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
    }

    void Update()
    {
        bool inputPressedThisFrame = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool inputConsumedThisFrame = false; 

        // GLOBAL CHECK: handle closing a letter that's already open
        Letter[] allLetters = FindObjectsByType<Letter>(FindObjectsSortMode.None);
        foreach (Letter l in allLetters)
        {
            if (l.IsReading)
            {
                if (uiText != null) uiText.text = l.GetPrompt();

                if (inputPressedThisFrame)
                {
                    l.CloseLetter();
                    inputConsumedThisFrame = true; // Consumes the input so the raycast below ignores it
                }

                return; // Block everything else while reading, INCLUDING the raycast below
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

            // 2. LETTER
            Letter letterScript = hit.collider.GetComponent<Letter>()
                ?? hit.collider.GetComponentInParent<Letter>();

            if (letterScript != null)
            {
                if (uiText != null) uiText.text = letterScript.GetPrompt();

                if (inputPressedThisFrame && !inputConsumedThisFrame)
                {
                    letterScript.OpenLetter();
                    return; // Prevent further execution on this specific frame
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