using UnityEngine;

public class CrowbarPickup : MonoBehaviour
{
    public string GetPickupPrompt()
    {
        return "Press E to pickup the crowbar";
    }

    public void InteractWithCrowbar(GameObject playerGameObject)
    {
        // Find inventory component automatically
        PlayerInventory inventory = playerGameObject.GetComponentInParent<PlayerInventory>() 
                                    ?? playerGameObject.GetComponentInChildren<PlayerInventory>()
                                    ?? playerGameObject.GetComponent<PlayerInventory>();
        
        if (inventory != null)
        {
            inventory.AddCrowbar();
        }

        // Find interaction component and display the hand model
        PlayerInteraction playerInteract = playerGameObject.GetComponentInParent<PlayerInteraction>()
                                           ?? playerGameObject.GetComponentInChildren<PlayerInteraction>()
                                           ?? playerGameObject.GetComponent<PlayerInteraction>();
        
        if (playerInteract != null)
        {
            playerInteract.ShowCrowbarInHand();
        }

        // Safely destroy only the floor asset
        Destroy(gameObject);
    }
}


