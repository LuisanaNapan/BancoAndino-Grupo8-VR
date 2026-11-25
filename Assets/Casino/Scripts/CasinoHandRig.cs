using UnityEngine;
using UnityEngine.Rendering;

public sealed class CasinoHandRig : MonoBehaviour
{
    static readonly Color Idle = new Color32(154, 211, 225, 255);
    static readonly Color Focus = new Color32(115, 247, 234, 255);
    static readonly Color Confirm = new Color32(255, 208, 113, 255);
    readonly Vector3[] ringPoints = new Vector3[41];
    MaterialPropertyBlock lineColor;
    Transform view, left, right, indexTip;
    LineRenderer beam, ring, progress;
    Material glove, cuff, signal;

    public void Initialize(Camera camera, Renderer originalReticle)
    {
        view = camera.transform;
        lineColor = new MaterialPropertyBlock();
        var source = originalReticle.sharedMaterial;
        glove = Tinted(source, new Color32(49, 79, 100, 255));
        cuff = Tinted(source, new Color32(85, 188, 196, 255));
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
        {
            glove.shader = lit;
            glove.SetColor("_BaseColor", new Color32(68, 102, 124, 255));
            glove.SetFloat("_Smoothness", .24f);
        }
        signal = Tinted(source, Color.white);
        originalReticle.enabled = false;
        left = CreateHand(-1);
        right = CreateHand(1);
        PoseHand(left, -1, 0, false);
        PoseHand(right, 1, 0, false);
        beam = Line("Haz de selección", .002f);
        ring = Line("Anillo de foco", .002f);
        progress = Line("Progreso de mirada", .0035f);
    }

    static Material Tinted(Material source, Color color)
    {
        var result = new Material(source);
        result.SetColor("_BaseColor", color);
        return result;
    }

    Transform CreateHand(int side)
    {
        var root = new GameObject(side < 0 ? "Mano izquierda" : "Mano derecha").transform;
        root.SetParent(view, false);
        Part(root, "Manga", new Vector3(0, -.006f, -.082f), new Vector3(.077f, .045f, .17f), glove);
        Part(root, "Marca del puño", new Vector3(0, .015f, -.067f), new Vector3(.061f, .008f, .031f), cuff);
        Part(root, "Palma", Vector3.zero, new Vector3(.077f, .039f, .116f), glove);
        for (int finger = 0; finger < 4; finger++)
        {
            float x = (finger - 1.5f) * .018f;
            bool pointing = side > 0 && finger == 0;
            var digit = Part(root, "Dedo " + (finger + 1),
                new Vector3(x, pointing ? .002f : -.014f, pointing ? .080f : .054f),
                new Vector3(.017f, .017f, pointing ? .086f : .063f), glove);
            digit.localRotation = Quaternion.Euler(pointing ? -5f : 25f, (finger - 1.5f) * 3f, 0);
            if (pointing)
            {
                indexTip = new GameObject("Origen del puntero").transform;
                indexTip.SetParent(digit, false);
                indexTip.localPosition = new Vector3(0, 0, .52f);
            }
        }
        var thumb = Part(root, "Pulgar", new Vector3(-side * .039f, -.005f, .024f),
            new Vector3(.022f, .025f, .058f), glove);
        thumb.localRotation = Quaternion.Euler(15, -side * 35, 0);
        return root;
    }

    static Transform Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = name;
        part.GetComponent<Collider>().enabled = false;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part.transform;
    }

    LineRenderer Line(string name, float width)
    {
        var node = new GameObject(name);
        node.transform.SetParent(transform, false);
        var line = node.AddComponent<LineRenderer>();
        line.sharedMaterial = signal;
        line.widthMultiplier = width;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = 0;
        return line;
    }

    public void Present(Ray sight, float distance, bool selectable, float dwell, bool confirmed)
    {
        if (ring == null) return;
        float step = Time.unscaledTime;
        PoseHand(left, -1, Mathf.Sin(step * 1.7f) * .006f, false);
        PoseHand(right, 1, Mathf.Sin(step * 1.9f) * .006f, confirmed);
        Color color = confirmed ? Confirm : selectable ? Focus : Idle;
        Vector3 endpoint = sight.GetPoint(distance);
        float radius = distance * (selectable ? .014f : .007f);
        ring.widthMultiplier = distance * .0018f;
        progress.widthMultiplier = distance * .0028f;
        for (int i = 0; i < ringPoints.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / (ringPoints.Length - 1);
            ringPoints[i] = endpoint + (view.up * Mathf.Cos(angle) + view.right * Mathf.Sin(angle)) * radius;
        }
        ring.positionCount = ringPoints.Length;
        ring.SetPositions(ringPoints);
        Paint(ring, color);
        progress.positionCount = dwell > 0 ? Mathf.CeilToInt(dwell * 40) + 1 : 0;
        for (int i = 0; i < progress.positionCount; i++) progress.SetPosition(i, ringPoints[i]);
        Paint(progress, Confirm);

        beam.enabled = right.gameObject.activeSelf && (selectable || confirmed) && distance > .5f;
        if (!beam.enabled) return;
        beam.positionCount = 2;
        beam.SetPosition(0, indexTip.position);
        beam.SetPosition(1, endpoint);
        Paint(beam, color);
    }

    void PoseHand(Transform hand, int side, float sway, bool pulse)
    {
        Vector3 rest = new Vector3(side * .31f, -.252f + sway + (pulse ? .022f : 0), .53f);
        Vector3 world = view.TransformPoint(rest);
        Vector3 reach = world - view.position;
        if (Physics.SphereCast(view.position, .065f, reach.normalized, out var hit, reach.magnitude, ~0, QueryTriggerInteraction.Ignore))
            rest *= Mathf.Clamp01((hit.distance - .055f) / reach.magnitude);
        hand.gameObject.SetActive(rest.z > .23f);
        hand.localPosition = Vector3.Lerp(hand.localPosition, rest, Time.unscaledDeltaTime * 14f);
        hand.localRotation = Quaternion.Euler(pulse ? -5 : 12, -side * 15, side * -12);
    }

    void Paint(LineRenderer line, Color color)
    {
        lineColor.SetColor("_BaseColor", color);
        line.SetPropertyBlock(lineColor);
    }

    void OnDestroy()
    {
        Destroy(glove);
        Destroy(cuff);
        Destroy(signal);
    }
}
