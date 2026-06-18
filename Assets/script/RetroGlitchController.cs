using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class RetroGlitchController : MonoBehaviour
{
    private Volume volume;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private ColorAdjustments colorAdjustments;

    void Start()
    {
        volume = GetComponent<Volume>();
        
        if (volume.profile.TryGet(out chromaticAberration) && 
            volume.profile.TryGet(out filmGrain) &&
            volume.profile.TryGet(out colorAdjustments))
        {
            StartCoroutine(GlitchLoop());
        }
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            // --- 1. SHORT TIME GAP ---
            // Breaks every 0.5 to 2 seconds instead of waiting forever
            float randomWait = Random.Range(0.5f, 2.0f);
            yield return new WaitForSeconds(randomWait);

            // --- 2. AGGRESSIVE MAX INTENSITY ---
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 5f; // Pushed past 1.0 for massive color separation

            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = 1f; // Maxed out noise

            // Flash the contrast/exposure randomly to make the screen flicker violently
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = Random.Range(-2f, 2f); 

            // --- 3. GLITCH DURATION ---
            float glitchDuration = Random.Range(0.05f, 0.15f);
            yield return new WaitForSeconds(glitchDuration);

            // --- 4. RESET TO YOUR PERFECT DEFAULT LOOK ---
            chromaticAberration.intensity.value = 0.2f; // Back to your default
            filmGrain.intensity.value = 1f;            // Keeping your nice grain look constant
            colorAdjustments.postExposure.value = 0f;    // Reset exposure back to normal
        }
    }
}