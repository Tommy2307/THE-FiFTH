using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HintDot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float showDistance = 3f;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float maxAlpha = 0.8f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        // Auto find player if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
    }

    void Update()
    {
        if (sr == null)
            return;

        if (player == null)
            return;

        if (transform.parent == null)
            return;

        float distance = Vector3.Distance(player.position, transform.parent.position);

        float targetAlpha = (distance <= showDistance) ? maxAlpha : 0f;

        Color c = sr.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        sr.color = c;

        // Always face the camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0f, 180f, 0f);
        }
    }
}