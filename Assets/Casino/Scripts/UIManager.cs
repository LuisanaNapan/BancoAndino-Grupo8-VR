using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public Text[] counterTexts,modeTexts,objectiveTexts;
    public Text infoText, incidentTitle, incidentInstruction;
    [System.NonSerialized] public Text terminalStatus;
    public Image incidentStrip;
    public Transform infoRoot;
    public Camera view;
    public Font font,displayFont;
    readonly List<string> pages=new List<string>();int page;
    CasinoFieldDisplay field;
    public string Reading => field == null ? "" : field.Reading;
    public string Subtitle => field == null ? "" : field.Subtitle;
    public bool InfoOpen {get{return infoRoot!=null&&infoRoot.gameObject.activeSelf;}}
    public void PrepareField(GameManager game)
    {
        field = gameObject.AddComponent<CasinoFieldDisplay>();
        field.Initialize(this, game);
        CloseInfo();
    }
    public void Read(string title, string text) => field.SetReading(title, text);
    public void Caption(string text, float duration, bool singleLine = false) => field.SetCaption(text, duration, singleLine);
    void Update()
    {
        var g=GameManager.I;if(g==null)return;
        foreach(var t in objectiveTexts)if(t!=null)t.text=g.Objective;
    }
    public void Show(string message)
    {
        if (field != null)
        {
            int firstLine = message.IndexOf('\n');
            Read(firstLine < 0 ? message : message.Substring(0, firstLine),
                firstLine < 0 ? "" : message.Substring(firstLine + 1));
            return;
        }
        pages.Clear();var lines=new List<string>();
        foreach(string paragraph in message.Split('\n'))
        {
            string line="";
            foreach(string word in paragraph.Split(' '))
            {
                if(line.Length+word.Length+1>48){lines.Add(line);line="";}
                line+=(line.Length>0?" ":"")+word;
            }
            if(line.Length>0)lines.Add(line);
        }
        for(int i=0;i<lines.Count;i+=7)pages.Add(string.Join("\n",lines.GetRange(i,Mathf.Min(7,lines.Count-i)).ToArray()));
        if(pages.Count==0)pages.Add("");page=0;
        Vector3 forward=view.transform.forward;forward.y=0;
        if(forward.sqrMagnitude<.001f)forward=Vector3.forward;forward.Normalize();
        infoRoot.position=view.transform.position+forward*2.1f+Vector3.up*.25f;
        infoRoot.rotation=Quaternion.LookRotation(forward);
        infoRoot.gameObject.SetActive(true);RenderPage();
    }
    void RenderPage(){infoText.text=pages[page]+(pages.Count>1?"\n"+(page+1)+" / "+pages.Count:"");}
    public void NextInfo(){if(page+1<pages.Count){page++;RenderPage();}else CloseInfo();}
    public void CloseInfo(){infoRoot.gameObject.SetActive(false);}
    public void Prompt(string text){} // No floating text over the table.
    public void Result(string text){}
    void RefreshIncident(GameManager g)
    {
        if (incidentTitle == null || incidentInstruction == null || incidentStrip == null) return;
        Color color = new Color32(255, 126, 128, 255);
        string title = "INCIDENTE EN CURSO";
        string instruction = "Flujo abierto · Casa de cambio → Casino Horizonte";
        if (g.Frozen)
        {
            color = new Color32(101, 240, 185, 255);
            title = "FONDOS RETENIDOS";
            instruction = $"{MoneyCounter.Short(g.counter.frozen)} retenidos · US$ 0 al casino";
        }
        else if (g.mode == SimMode.Defensa)
        {
            color = new Color32(94, 229, 255, 255);
            title = "INVESTIGACIÓN ABIERTA";
            bool detected = g.HasEvidence("libro")
                && StructuringDetector.Detect(g.data.operations, g.data.demonstrationThreshold, out _, out _);
            string pattern = detected ? "Patrón detectado" : g.HasEvidence("libro") ? "Sin patrón" : "Patrón pendiente";
            instruction = pattern + (g.HasEvidence("alerta") ? " · Alerta verificada" : " · Alerta pendiente");
        }
        incidentTitle.text = title;
        incidentTitle.color = color;
        incidentInstruction.text = instruction;
        incidentStrip.color = color;
    }

    public void Refresh(GameManager g)
    {
        RefreshIncident(g);
        if (terminalStatus != null)
            terminalStatus.text = g.HasEvidence("alerta") ? "Aviso revisado" : "Alerta recibida";
        foreach(var t in counterTexts)if(t!=null)t.text=g.counter.Summary(g.data);
        bool defense = g.mode == SimMode.Defensa;
        int reviewed = defense
            ? (g.HasEvidence("libro") ? 1 : 0) + (g.HasEvidence("alerta") ? 1 : 0)
            : g.EvidenceCount;
        foreach (var t in modeTexts)
            if (t != null) t.text = g.mode.ToString().ToUpperInvariant() + " / Pistas " + reviewed + " de " + (defense ? 2 : 3);
    }
}
