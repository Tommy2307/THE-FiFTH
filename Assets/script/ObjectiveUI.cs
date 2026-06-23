using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    void Start()
    {
        if (objectiveText == null)
        {
            objectiveText = GetComponent<TextMeshProUGUI>();
        }

        if (objectiveText != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.RegisterObjectiveText(objectiveText);
        }
    }
}