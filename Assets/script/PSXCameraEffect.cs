using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PSXCameraEffect : MonoBehaviour
{
    [Header("PSX Settings")]
    [SerializeField] private Material psxMaterial;
    [SerializeField] private int renderWidth = 320;
    [SerializeField] private int renderHeight = 240;

    private RenderTexture lowResTexture;

    void Start()
    {
        lowResTexture = new RenderTexture(renderWidth, renderHeight, 16);
        lowResTexture.filterMode = FilterMode.Point;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (psxMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, lowResTexture);
        Graphics.Blit(lowResTexture, destination, psxMaterial);
    }

    void OnDestroy()
    {
        if (lowResTexture != null)
            lowResTexture.Release();
    }
}