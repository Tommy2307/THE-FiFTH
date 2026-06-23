using UnityEngine;

public class IDCardPickup : MonoBehaviour
{
    private bool isCollected = false;

    public string GetPickupPrompt()
    {
        return "Press E to Inspect";
    }

    public void InteractWithIDCard(GameObject playerGameObject)
    {
        if (isCollected) return;
        isCollected = true;

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.StartInspectDialogue(() =>
            {
                // Callback runs after the full dialogue finishes
                PlayerInventory inventory = playerGameObject.GetComponentInParent<PlayerInventory>() 
                                            ?? playerGameObject.GetComponentInChildren<PlayerInventory>()
                                            ?? playerGameObject.GetComponent<PlayerInventory>();
                
                if (inventory != null)
                {
                    inventory.AddIDCard();
                }

                PlayerInteraction playerInteract = playerGameObject.GetComponentInParent<PlayerInteraction>()
                                                   ?? playerGameObject.GetComponentInChildren<PlayerInteraction>()
                                                   ?? playerGameObject.GetComponent<PlayerInteraction>();
                
                if (playerInteract != null)
                {
                    playerInteract.ShowIDCardInHand();
                }

                // Destroy the physical card object
                Destroy(gameObject);
            });
        }
        else
        {
            // Fallback if ObjectiveManager is missing
            Destroy(gameObject);
        }
    }
}