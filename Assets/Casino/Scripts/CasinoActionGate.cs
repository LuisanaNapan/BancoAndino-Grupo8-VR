using UnityEngine;
using UnityEngine.UI;

public class CasinoActionGate : MonoBehaviour
{
    public bool onlyWhenComplete,gated=true;
    public CasinoInteractable target;
    public GameObject visuals;
    public Collider hit;
    public Image background;
    public Text label;
    public Color normal;
    public string caption;
    void Update()
    {
        var g=GameManager.I;if(g==null)return;
        bool visible=onlyWhenComplete?g.Completed:(!gated||g.VisibleAction(target.action));bool enabled=!gated||g.EnabledAction(target.action);
        if(visuals.activeSelf!=visible)visuals.SetActive(visible);
        hit.enabled=visible&&enabled;
        if(background!=null&&!enabled)background.color=new Color(.12f,.17f,.18f);
        else if(background!=null)background.color=target.IsFocused?target.focusColor:normal;
        if(label!=null)
        {
            label.text=caption;
            if(target.action==CasinoAction.PlaceBank&&g.BankPlaced)label.text="BANCA: LISTO";
            if(target.action==CasinoAction.PlacePlayer&&g.PlayerPlaced)label.text="JUGADOR: LISTO";
        }
    }
}
