using UnityEngine;
using UnityEngine.UI;

// Both the checked-in scene and the scene generator use these same room treatments.
public static class CasinoOperationsRoom
{
    public static void Install(GameManager game)
    {
        foreach (GameObject root in game.gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "Casino_Arte") DressRooms(root.transform, game);
            if (!root.TryGetComponent<Light>(out var light) || light.type != LightType.Point) continue;
            bool casino = root.transform.position.x > game.exchangeSpawn.position.x + 10f;
            light.color = casino ? new Color(1f, .79f, .63f) : new Color(.65f, .85f, 1f);
            light.intensity = casino ? 2.2f : 2.05f;
        }
    }

    static void DressRooms(Transform roomArt, GameManager game)
    {
        Transform firstMenu = null;
        foreach (Transform item in roomArt)
        {
            bool casino = item.position.x > game.exchangeSpawn.position.x + 10f;
            Color accent = casino ? new Color32(255, 201, 131, 255) : new Color32(109, 224, 228, 255);
            switch (item.name)
            {
                case "Estado_del_caso":
                    item.gameObject.SetActive(false);
                    break;
                case "Monitor_del_incidente":
                    EditMonitor(item, casino, accent);
                    break;
                case "Consola_Analisis":
                case "Consola_Mesa":
                    EditConsole(item, casino, accent);
                    break;
                case "Menu_recorrido":
                    InstallAudioMenu(item, accent);
                    if (!casino) firstMenu = item;
                    break;
                case "Pista_alerta":
                    InstallTerminal(item, game.ui, accent);
                    break;
                case "Rol_Analista":
                    item.GetComponentInChildren<Text>().text = "SOBRE EL CASO";
                    break;
            }
        }
        if (firstMenu != null) InstallModePicker(firstMenu, roomArt, game);
    }

    static void InstallModePicker(Transform menu, Transform roomArt, GameManager game)
    {
        var chooser = new GameObject("Elección de recorrido", typeof(RectTransform)).GetComponent<RectTransform>();
        chooser.SetParent(roomArt, false);
        chooser.position = game.exchangeSpawn.position + new Vector3(0, .06f, 2.05f);
        chooser.localScale = Vector3.one * .0017f;
        chooser.sizeDelta = new Vector2(990, 275);
        var canvas = chooser.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 3;
        var panel = chooser.gameObject.AddComponent<Image>();
        panel.color = new Color32(8, 33, 44, 255);
        panel.raycastTarget = false;
        TerminalLabel(chooser, game.ui.displayFont, "ELIGE RECORRIDO", 78, 49, new Color32(109, 224, 228, 255));
        foreach (var control in menu.GetComponents<CasinoActionGate>())
        {
            CasinoAction action = control.target.action;
            if (action != CasinoAction.Attack && action != CasinoAction.Defense) continue;
            var copy = Object.Instantiate(control.visuals, chooser);
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = new Vector2(action == CasinoAction.Attack ? -239f : 239f, -51f);
            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, -22);
            rect.localScale = new Vector3(.50f, .85f, 1f);
            var text = copy.GetComponentInChildren<Text>().rectTransform;
            text.localScale = new Vector3(1.6f, 1f, 1f);
            text.sizeDelta = new Vector2(460f, 110f);
        }
        game.ui.modePicker = chooser.gameObject;
    }

    static void InstallTerminal(Transform target, UIManager ui, Color accent)
    {
        var screen = new GameObject("Terminal del banco", typeof(RectTransform)).GetComponent<RectTransform>();
        screen.SetParent(target, false);
        screen.position = target.position + new Vector3(0, .26f, .14f);
        screen.rotation = Quaternion.identity;
        screen.localScale = Vector3.one * .001f;
        screen.sizeDelta = new Vector2(797, 515);
        screen.gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var fill = screen.gameObject.AddComponent<Image>();
        fill.color = new Color32(8, 33, 44, 255);
        fill.raycastTarget = false;
        TerminalLabel(screen, ui.displayFont, "MONITOREO DEL BANCO", 132, 46, accent);
        TerminalLabel(screen, ui.displayFont, "OR-08", 6, 90, Color.white);
        ui.terminalStatus = TerminalLabel(screen, ui.font, "Alerta recibida", -136, 50, accent);
    }

    static Text TerminalLabel(RectTransform screen, Font font, string text, float height, int size, Color color)
    {
        var rect = new GameObject(text, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(screen, false);
        rect.anchoredPosition = new Vector2(0, height);
        rect.sizeDelta = new Vector2(760, 112);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    static void EditMonitor(Transform monitor, bool casino, Color accent)
    {
        if (monitor.TryGetComponent<Image>(out var background))
            background.color = casino ? new Color32(49, 26, 37, 255) : new Color32(8, 23, 36, 255);
        foreach (var label in monitor.GetComponentsInChildren<Text>())
        {
            if (label.text.Contains("HORIZONTE")) label.text = "CASINO HORIZONTE";
            if (label.text.Contains("BANCO ANDINO")) label.text = "BANCO ANDINO · CENTRO DE ANÁLISIS";
            if (label.text.Contains("01 FICHAS")) label.text = "PESOS  →  FICHAS  →  CAJA";
            if (label.text.Contains("HORIZONTE") || label.text.Contains("CENTRO") || label.text.Contains("PESOS"))
                label.color = accent;
        }
    }

    static void EditConsole(Transform console, bool casino, Color accent)
    {
        if (console.TryGetComponent<Image>(out var background))
            background.color = casino ? new Color32(40, 23, 35, 255) : new Color32(8, 27, 42, 255);
        foreach (var label in console.GetComponentsInChildren<Text>(true))
        {
            if (label.text.StartsWith("Mira un control")) label.gameObject.SetActive(false);
            if (label.text == "CONTROL DE FONDOS") label.text = "ESTACIÓN DE OPERACIONES";
            if (label.text == "ESTACIÓN DE OPERACIONES" || label.text == "MESA DE BACARÁ") label.color = accent;
        }
    }

    static void InstallAudioMenu(Transform menu, Color accent)
    {
        menu.position += Vector3.up * .24f;
        var controls = menu.GetComponentsInChildren<CasinoActionGate>(true);
        CasinoActionGate replay = null;
        foreach (var control in controls)
        {
            var button = (RectTransform)control.target.transform;
            switch (control.target.action)
            {
                case CasinoAction.Attack: button.anchoredPosition = new Vector2(0, 315); break;
                case CasinoAction.Defense: button.anchoredPosition = new Vector2(0, 155); break;
                case CasinoAction.Restart: button.anchoredPosition = new Vector2(0, -5); break;
                case CasinoAction.ToggleMusic: button.anchoredPosition = new Vector2(0, -165); control.caption = "MÚSICA"; break;
                case CasinoAction.ReplayGuide: button.anchoredPosition = new Vector2(0, -325); replay = control; break;
            }
        }
        if (replay == null) return;
        AddAudioControl(replay, CasinoAction.MuteNarration, 0f, .48f, accent);
        AddAudioControl(replay, CasinoAction.VoiceDown, -323f, .24f, accent);
        AddAudioControl(replay, CasinoAction.VoiceUp, 323f, .24f, accent);
    }

    static void AddAudioControl(CasinoActionGate template, CasinoAction action, float x, float width, Color accent)
    {
        var button = Object.Instantiate(template.visuals, template.transform);
        button.name = action.ToString();
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(x, -500f);
        rect.localScale = new Vector3(width, 1, 1);
        var control = template.gameObject.AddComponent<CasinoActionGate>();
        control.visuals = button;
        control.target = button.GetComponent<CasinoInteractable>();
        control.hit = button.GetComponent<Collider>();
        control.background = button.GetComponent<Image>();
        control.normal = template.normal;
        control.gated = false;
        control.label = button.GetComponentInChildren<Text>();
        control.label.rectTransform.localScale = new Vector3(1f / width, 1, 1);
        control.label.rectTransform.sizeDelta = new Vector2(800f * width, 110f);
        control.target.action = action;
        control.target.prompt = action == CasinoAction.VoiceUp ? "Subir voz" : action == CasinoAction.VoiceDown ? "Bajar voz" : "Silenciar o activar voz";
        control.caption = action == CasinoAction.VoiceUp ? "+" : action == CasinoAction.VoiceDown ? "−" : "VOZ";
        control.label.color = accent;
    }
}
