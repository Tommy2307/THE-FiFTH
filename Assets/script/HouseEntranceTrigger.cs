using UnityEngine;

public class HouseEntranceTrigger : MonoBehaviour
{
    [SerializeField] private GameObject outsideDust;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        outsideDust.SetActive(false);

        Destroy(gameObject);
    }
}