using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public Text[] counterTexts,modeTexts,objectiveTexts;
    public Text infoText;
    public Transform infoRoot;
    public Camera view;
    public Font font,displayFont;
    readonly List<string> pages=new List<string>();int page;
    public bool InfoOpen {get{return infoRoot!=null&&infoRoot.gameObject.activeSelf;}}
    void Update()
    {
        var g=GameManager.I;if(g==null)return;
        foreach(var t in objectiveTexts)if(t!=null)t.text=g.Objective;
    }
    public void Show(string message)
    {
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
    public void Refresh(GameManager g)
    {
        foreach(var t in counterTexts)if(t!=null)t.text=g.counter.Summary(g.data);
        foreach(var t in modeTexts)if(t!=null)t.text=g.mode.ToString().ToUpperInvariant()+" / Pistas "+g.EvidenceCount+" de 3";
    }
}
