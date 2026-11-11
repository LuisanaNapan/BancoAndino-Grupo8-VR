using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource effects, ambience;
    public AudioClip selectClip, alertClip, chipsClip, endingClip, paperClip, registerClip;
    bool muted;
    public void Select(){Play(selectClip);}
    public void Alert(){Play(alertClip);}
    public void Chips(){Play(chipsClip);}
    public void Ending(){Play(endingClip);}
    public void ToggleMusic(){muted=!muted;if(ambience!=null)ambience.mute=muted;}
    public void SelectAction(CasinoAction action)
    {
        if(action==CasinoAction.Ledger||action==CasinoAction.Receipt)Play(paperClip);
        else if(action==CasinoAction.Convert||action==CasinoAction.CashOut)Play(registerClip);
        else Select();
    }
    void Play(AudioClip clip){if(effects!=null&&clip!=null)effects.PlayOneShot(clip,.55f);}
}
