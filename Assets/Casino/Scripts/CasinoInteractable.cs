using UnityEngine;

public enum CasinoAction { Ledger, Receipt, SharedAlert, Convert, Report, ToCasino, ToExchange, Bet, CashOut, Finish, Attack, Defense, Restart, Credits, ExplainCorrect, ExplainWrong, Approach, Overview, ToggleMusic, PlaceBank, PlacePlayer, Deal, Host, InfoNext, InfoClose, ToConsole }

public class CasinoInteractable : MonoBehaviour
{
    public CasinoAction action;
    public string prompt;
    public UnityEngine.UI.Image uiFace;
    public Color idleColor, focusColor;
    public bool IsFocused { get; private set; }
    public Renderer face;
    public GameObject guideMarker;
    public GameObject focusDecoration;
    public Material idleMaterial;
    public Material focusMaterial;
    public void Focus(bool value)
    {
        IsFocused=value;
        if(uiFace!=null)uiFace.color=value?focusColor:idleColor;
        if (focusDecoration != null) focusDecoration.SetActive(value);
        if (face != null) face.sharedMaterial = value ? focusMaterial : idleMaterial;
    }
    public void Activate() { if (GameManager.I != null) GameManager.I.Act(action); }
}
