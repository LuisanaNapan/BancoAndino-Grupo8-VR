using UnityEngine;

// Fit in local space so camera rotation and stereo views do not change text size.
[ExecuteAlways]
[RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
public class WorldTextFit : MonoBehaviour
{
    public float maxWidth = 9f;
    public float maxHeight = 2f;
    public float lineHeight = 0.26f;
    TextMesh textMesh;
    MeshRenderer meshRenderer;
    string lastText;
    Vector3 lastBounds;
    void LateUpdate()
    {
        if (textMesh == null) textMesh = GetComponent<TextMesh>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        Vector3 size = meshRenderer.localBounds.size;
        if (size.x < 0.0001f || size.y < 0.0001f) return;
        if (lastText == textMesh.text && (lastBounds-size).sqrMagnitude < .000001f) return;
        lastText=textMesh.text; lastBounds=size;
        int lines = 1;
        foreach (char c in textMesh.text) if (c == '\n') lines++;
        float targetHeight = Mathf.Min(maxHeight, lineHeight * lines);
        float scale = Mathf.Min(maxWidth / size.x, targetHeight / size.y);
        transform.localScale = Vector3.one * scale;
    }
}
