using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private KeyType currentKey;
    private bool hasAnyKey = false;

    [Header("Hand Visual References")]
    [SerializeField] private GameObject handKey1Visual;
    [SerializeField] private GameObject handKey2Visual;
    [SerializeField] private GameObject handFinalKeyVisual;
    [SerializeField] private GameObject handKey3Visual;
    [SerializeField] private GameObject handCrowbarVisual;  
    [SerializeField] private GameObject handIDCardVisual;   // Added ID Card visual in hand

    // Tools & Quest Items tracking fields
    private bool hasCrowbar = false;
    private bool hasIDCard = false;                         // Added tracking flag

    void Start()
    {
        // Automatically search references if left unassigned
        if (handKey1Visual == null)     handKey1Visual = FindChildByName("handkey 1");
        if (handKey2Visual == null)     handKey2Visual = FindChildByName("handkey 2");
        if (handKey3Visual == null)     handKey3Visual = FindChildByName("handkey 3");
        if (handCrowbarVisual == null)  handCrowbarVisual = FindChildByName("Hand Crowbar");
        if (handIDCardVisual == null)   handIDCardVisual = FindChildByName("Hand ID Card");

        // Hide all visuals on game initialization
        if (handKey1Visual != null)     handKey1Visual.SetActive(false);
        if (handKey2Visual != null)     handKey2Visual.SetActive(false);
        if (handFinalKeyVisual != null) handFinalKeyVisual.SetActive(false);
        if (handKey3Visual != null)     handKey3Visual.SetActive(false);
        if (handCrowbarVisual != null)  handCrowbarVisual.SetActive(false);
        if (handIDCardVisual != null)   handIDCardVisual.SetActive(false);
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

    // ── Existing Key Logic ───────────────────────────────────────────

    public bool HasKey(KeyType keyType)
    {
        return hasAnyKey && currentKey == keyType;
    }

    public bool IsInventoryEmpty()
    {
        return !hasAnyKey;
    }

    public bool AddKey(KeyType keyType)
    {
        if (!IsInventoryEmpty())
        {
            Debug.LogWarning("[INVENTORY] Cannot pick up! Player is already holding a key.");
            return false;
        }

        currentKey = keyType;
        hasAnyKey = true;

        Debug.Log($"[INVENTORY] Added {keyType}!");
        UpdateHandVisuals(keyType, true);

        return true;
    }

    public void UseKey(KeyType keyType)
    {
        if (hasAnyKey && currentKey == keyType)
        {
            hasAnyKey = false;
            Debug.Log($"[INVENTORY] Used {keyType}!");
            UpdateHandVisuals(keyType, false);
        }
    }

    // ── Crowbar Logic ────────────────────────────────────────────────

    public bool HasCrowbar()
    {
        return hasCrowbar;
    }

    public void AddCrowbar()
    {
        hasCrowbar = true;
        Debug.Log("[INVENTORY] Crowbar picked up!");
        if (handCrowbarVisual != null) handCrowbarVisual.SetActive(true);
    }

    public void RemoveCrowbar()
    {
        hasCrowbar = false;
        Debug.Log("[INVENTORY] Crowbar used!");
        if (handCrowbarVisual != null) handCrowbarVisual.SetActive(false);

        PlayerInteraction playerInteract = GetComponent<PlayerInteraction>() 
                                           ?? GetComponentInParent<PlayerInteraction>()
                                           ?? GetComponentInChildren<PlayerInteraction>();
        if (playerInteract != null)
        {
            playerInteract.HideCrowbarInHand();
        }
    }

    public void DropCrowbar()
    {
        if (!hasCrowbar) return;

        hasCrowbar = false;
        Debug.Log("[INVENTORY] Crowbar dropped!");

        if (handCrowbarVisual != null)
        {
            handCrowbarVisual.SetActive(false);

            PlayerInteraction playerInteract = GetComponent<PlayerInteraction>() 
                                               ?? GetComponentInParent<PlayerInteraction>()
                                               ?? GetComponentInChildren<PlayerInteraction>();
            if (playerInteract != null)
            {
                playerInteract.HideCrowbarInHand();
            }

            Camera mainCam = Camera.main;
            Vector3 spawnPos = mainCam != null 
                ? mainCam.transform.position + mainCam.transform.forward * 0.8f
                : transform.position + transform.forward * 0.8f + transform.up * 0.8f;
                
            Quaternion spawnRot = mainCam != null
                ? mainCam.transform.rotation * Quaternion.Euler(0, 90f, 0)
                : transform.rotation * Quaternion.Euler(0, 90f, 0);

            GameObject dropped = Instantiate(handCrowbarVisual, spawnPos, spawnRot);
            dropped.name = "DroppedCrowbar";
            dropped.SetActive(true);

            BoxCollider collider = dropped.GetComponent<BoxCollider>();
            if (collider == null) collider = dropped.AddComponent<BoxCollider>();
            collider.isTrigger = false;

            Rigidbody rb = dropped.GetComponent<Rigidbody>();
            if (rb == null) rb = dropped.AddComponent<Rigidbody>();

            Vector3 throwDir = mainCam != null ? mainCam.transform.forward : transform.forward;
            Vector3 throwForce = throwDir * 3.5f + Vector3.up * 1.5f; 
            
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.AddForce(throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * 3.0f, ForceMode.Impulse);
        }
    }

    // ── ID Card Logic ────────────────────────────────────────────────

    public bool HasIDCard()
    {
        return hasIDCard;
    }

    public void AddIDCard()
    {
        hasIDCard = true;
        Debug.Log("[INVENTORY] ID Card picked up!");
        if (handIDCardVisual != null) handIDCardVisual.SetActive(true);
    }

    public void RemoveIDCard()
    {
        hasIDCard = false;
        Debug.Log("[INVENTORY] ID Card removed!");
        if (handIDCardVisual != null) handIDCardVisual.SetActive(false);
    }

    // ── Visuals Management ───────────────────────────────────────────

    private void UpdateHandVisuals(KeyType keyType, bool show)
    {
        if (keyType == KeyType.Key1 && handKey1Visual != null)
            handKey1Visual.SetActive(show);
        else if (keyType == KeyType.Key2 && handKey2Visual != null)
            handKey2Visual.SetActive(show);
        else if (keyType == KeyType.FinalKey && handFinalKeyVisual != null)
            handFinalKeyVisual.SetActive(show);
        else if (keyType == KeyType.Key3 && handKey3Visual != null)
        {
            Debug.Log("KEY3 VISUAL ENABLED");
            handKey3Visual.SetActive(show);
        }
    }
}