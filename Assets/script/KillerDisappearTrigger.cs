using UnityEngine;

public class KillerDisappearTrigger : MonoBehaviour
{
    public GameObject killer;
    public AudioSource disappearSound;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            disappearSound.Play();
            killer.SetActive(false);
            Destroy(gameObject);
        }
    }
}