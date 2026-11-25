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
            string current=target.action==CasinoAction.MuteNarration ? g.VoiceStatus : caption;
            if(target.action==CasinoAction.Finish&&g.ExplanationAccepted)current="CERRAR CASO";
            if(target.action==CasinoAction.PlaceBank&&g.BankPlaced)current="BANCA: LISTO";
            if(target.action==CasinoAction.PlacePlayer&&g.PlayerPlaced)current="JUGADOR: LISTO";
            if(label.text!=current)label.text=current;
        }
    }
}
