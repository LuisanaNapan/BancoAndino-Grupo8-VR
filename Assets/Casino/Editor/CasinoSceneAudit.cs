using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CasinoSceneAudit
{
    [MenuItem("Casino 8/Comprobar disposicion")]
    public static void Check()
    {
        var gates=UnityEngine.Object.FindObjectsByType<CasinoActionGate>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        int checkedControls=0;
        foreach(var gate in gates)
        {
            if(gate.target==null||gate.label==null||gate.hit==null)throw new InvalidOperationException("Control incompleto: "+gate.name);
            if(gate.label.font==null)throw new InvalidOperationException("Fuente ausente: "+gate.name);
            var rt=gate.visuals.GetComponent<RectTransform>();var parent=(RectTransform)rt.parent;
            var bounds=new Rect(rt.anchoredPosition-rt.sizeDelta*.5f,rt.sizeDelta);
            Rect allowed=parent.rect;
            if(bounds.xMin<allowed.xMin||bounds.xMax>allowed.xMax||bounds.yMin<allowed.yMin||bounds.yMax>allowed.yMax)
                throw new InvalidOperationException("Control fuera del panel: "+gate.visuals.name);
            if(gate.label.preferredWidth>gate.label.rectTransform.rect.width+2)throw new InvalidOperationException("Rotulo demasiado ancho: "+gate.caption);
            if(!gate.visuals.activeInHierarchy)continue;
            foreach(var other in gates)
            {
                if(other==gate||!other.visuals.activeInHierarchy||other.visuals.transform.parent!=rt.parent)continue;
                var b=other.visuals.GetComponent<RectTransform>();var r=new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta);
                if(bounds.Overlaps(r))throw new InvalidOperationException("Controles superpuestos: "+gate.visuals.name+" / "+other.visuals.name);
            }
            checkedControls++;
        }
        if(UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)
            throw new InvalidOperationException("Quedo texto del sistema anterior en la escena.");
        Debug.Log("V5: disposicion revisada; "+checkedControls+" controles visibles sin solapamiento dentro de sus paneles. Falta revision visual en Game/visor.");
    }
}
