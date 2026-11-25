using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CasinoFieldDisplay : MonoBehaviour
{
    static readonly Color Ink = new Color32(7, 19, 30, 246);
    static readonly Color Paper = new Color32(235, 249, 255, 255);
    static readonly Color Signal = new Color32(109, 224, 228, 255);
    GameManager game;
    Camera view;
    Text objective, readingTitle, reading, caption;
    RectTransform canvas, document;
    Material fieldMaterial;
    GameObject readingPanel;
    readonly List<string> captionPages = new List<string>();
    float captionStart, captionDuration;
    int activePage;

    public string Reading => reading.text;
    public string Subtitle => caption.text;

    public void Initialize(UIManager ui, GameManager manager)
    {
        game = manager;
        view = ui.view;
        canvas = Rect(view.transform, "Guía de campo", new Vector2(900, 580));
        var worldCanvas = canvas.gameObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 80;
        var shader = Resources.Load<Shader>("CasinoFieldUI");
        if (shader != null) fieldMaterial = new Material(shader);
        canvas.localScale = Vector3.one * .00145f;
        canvas.localPosition = new Vector3(0, -.015f, 1.45f);

        var taskPanel = Panel(canvas, "Objetivo", new Vector2(0, 332), new Vector2(760, 60));
        objective = Label(taskPanel, ui.displayFont, Vector2.zero, new Vector2(720, 54), 25, Signal);
        var textPanel = Panel(canvas, "Subtítulos", new Vector2(0, -235), new Vector2(870, 100));
        caption = Label(textPanel, ui.font, Vector2.zero, new Vector2(824, 88), 27, Paper);
        var doc = Panel(canvas, "Evidencia", new Vector2(445, -52), new Vector2(680, 250));
        document = doc;
        readingTitle = Label(doc, ui.displayFont, new Vector2(0, 96), new Vector2(610, 42), 24, Signal);
        reading = Label(doc, ui.font, new Vector2(0, -16), new Vector2(605, 178), 24, Paper);
        reading.alignment = TextAnchor.MiddleLeft;
        readingPanel = doc.gameObject;
        readingPanel.SetActive(false);
        textPanel.gameObject.SetActive(false);
        FitDocument();
    }

    public void SetReading(string title, string body)
    {
        readingTitle.text = title;
        reading.text = body;
        readingPanel.SetActive(!string.IsNullOrEmpty(title));
    }

    public void SetCaption(string text, float duration, bool singleLine)
    {
        captionPages.Clear();
        string page = "";
        if (singleLine) captionPages.Add(text);
        else foreach (string word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (page.Length + word.Length > 99)
            {
                captionPages.Add(page);
                page = "";
            }
            page += (page.Length == 0 ? "" : " ") + word;
            if (word.EndsWith(".") && page.Length > 48)
            {
                captionPages.Add(page);
                page = "";
            }
        }
        if (page.Length > 0) captionPages.Add(page);
        captionStart = Time.unscaledTime;
        captionDuration = Mathf.Max(duration, captionPages.Count * 2.8f);
        activePage = -1;
        caption.transform.parent.gameObject.SetActive(captionPages.Count > 0);
        caption.text = "";
        UpdateCaption();
    }

    void Update()
    {
        string current = game.Objective;
        if (objective.text != current) objective.text = current;
        UpdateCaption();
    }

    void LateUpdate() { if (readingPanel.activeSelf) FitDocument(); }

    void FitDocument()
    {
        float aspect = view.stereoEnabled ? Mathf.Min(view.aspect, .95f) : view.aspect;
        float halfWidth = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f) * aspect
            * canvas.localPosition.z / canvas.localScale.x;
        var position = document.anchoredPosition;
        position.x = Mathf.Clamp(halfWidth * .94f - document.rect.width / 2f, 0f, 445f);
        if (position != document.anchoredPosition) document.anchoredPosition = position;
    }

    void UpdateCaption()
    {
        if (captionPages.Count == 0) return;
        float progress = (Time.unscaledTime - captionStart) / captionDuration;
        if (progress >= 1f)
        {
            caption.transform.parent.gameObject.SetActive(false);
            return;
        }
        int page = Mathf.Min(captionPages.Count - 1, (int)(progress * captionPages.Count));
        if (page == activePage) return;
        activePage = page;
        caption.text = captionPages[page];
    }

    static RectTransform Rect(Transform parent, string name, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        return rect;
    }

    RectTransform Panel(Transform parent, string name, Vector2 at, Vector2 size)
    {
        var rect = Rect(parent, name, size);
        rect.anchoredPosition = at;
        var bg = rect.gameObject.AddComponent<Image>();
        bg.color = Ink;
        if (fieldMaterial != null) bg.material = fieldMaterial;
        bg.raycastTarget = false;
        return rect;
    }

    Text Label(Transform parent, Font font, Vector2 at, Vector2 size, int pixels, Color color)
    {
        var rect = Rect(parent, "Texto", size);
        rect.anchoredPosition = at;
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = pixels;
        label.color = color;
        if (fieldMaterial != null) label.material = fieldMaterial;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 19;
        label.resizeTextMaxSize = pixels;
        return label;
    }

    void OnDestroy() { if (fieldMaterial != null) Destroy(fieldMaterial); }
}
