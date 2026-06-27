using UnityEngine;
using TMPro;
using System.Collections; // Fixed: Removed the broken '.Empty' line!

public class TutorialUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("Timer Settings")]
    [SerializeField] private float displayDuration = 5f; 
    [SerializeField] private float fadeDuration = 1.5f;   

    void Start()
    {
        if (tutorialText == null)
        {
            tutorialText = GetComponent<TextMeshProUGUI>();
        }

        if (tutorialText != null)
        {
            tutorialText.text = "Press F to turn on the Flashlight,";
            
            Color txtColor = tutorialText.color;
            txtColor.a = 1f;
            tutorialText.color = txtColor;

            StartCoroutine(DisplayAndFadeTutorial());
        }
    }

    private IEnumerator DisplayAndFadeTutorial()
    {
        yield return new WaitForSeconds(displayDuration);

        float currentTime = 0f;
        Color startColor = tutorialText.color;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);
            tutorialText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        tutorialText.gameObject.SetActive(false);
    }
}