using UnityEngine;
using TMPro;

public class TurnDisplayTMPro : MonoBehaviour
{
    private TextMeshPro textMesh;
    private TextMeshProUGUI textMeshGui;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        textMeshGui = GetComponent<TextMeshProUGUI>();
    }

    public void setText(string text)
    {
        if (textMesh != null) textMesh.text = text;
        if (textMeshGui != null) textMeshGui.text = text;
    }
}
