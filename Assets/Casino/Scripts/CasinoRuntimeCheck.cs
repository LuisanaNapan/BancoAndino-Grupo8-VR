using System;
using System.Collections;
using UnityEngine;

public class CasinoRuntimeCheck : MonoBehaviour
{
    static bool running;
    public static void Begin()
    {
        if(!Application.isPlaying){Debug.LogWarning("Pulsa Play antes de iniciar la prueba. La prueba reinicia el recorrido.");return;}
        if(running)return;
        var go=new GameObject("Comprobacion_V5");go.AddComponent<CasinoRuntimeCheck>().StartCoroutine(Guard(go));
    }
    static IEnumerator Guard(GameObject owner)
    {
        var test=Run(owner);
        while(true)
        {
            bool more=false,failed=false;object current=null;
            try{more=test.MoveNext();if(more)current=test.Current;}
            catch(Exception error){Debug.LogException(error);failed=true;running=false;Destroy(owner);}
            if(failed||!more)yield break;
            yield return current;
        }
    }
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("PRUEBA V5: "+message);}
    static IEnumerator Run(GameObject owner)
    {
        running=true;yield return null;var g=GameManager.I;
        if(g==null){Debug.LogError("No esta abierta la escena Casino_VR.");running=false;Destroy(owner);yield break;}
        g.ResetScenario(SimMode.Ataque);g.ui.CloseInfo();
        g.Act(CasinoAction.ToCasino);g.ui.CloseInfo();Require(!g.InCasino,"portal abierto sin evidencias");
        foreach(var a in new[]{CasinoAction.Ledger,CasinoAction.Receipt,CasinoAction.SharedAlert}){g.Act(a);g.ui.CloseInfo();}
        Require(g.EvidenceCount==3,"no registra las tres pistas");g.Act(CasinoAction.Convert);g.ui.CloseInfo();g.Act(CasinoAction.ToCasino);g.ui.CloseInfo();
        Require(g.InCasino,"no permite entrar tras completar los requisitos");g.Act(CasinoAction.ToConsole);
        g.Act(CasinoAction.PlaceBank);
        float timeout=Time.time+8;while(g.chipAnimator.running&&Time.time<timeout)yield return null;
        Require(g.BankPlaced,"no termina la colocacion en Banca");g.Act(CasinoAction.PlacePlayer);
        timeout=Time.time+8;while(g.chipAnimator.running&&Time.time<timeout)yield return null;
        Require(g.PlayerPlaced,"no termina la colocacion en Jugador");yield return null;
        // Use the real physics pick path to verify that another collider does not intercept Reveal.
        var controls=FindObjectsByType<CasinoInteractable>(FindObjectsSortMode.None);CasinoInteractable reveal=null;
        foreach(var c in controls)if(c.action==CasinoAction.Deal)reveal=c;
        Require(reveal!=null,"no aparece el control Revelar");Physics.SyncTransforms();
        var view=g.ui.view;Vector3 delta=reveal.GetComponent<Collider>().bounds.center-view.transform.position;RaycastHit hit;
        Require(Physics.Raycast(view.transform.position,delta.normalized,out hit,delta.magnitude+.2f),"la seleccion no llega a Revelar");
        Require(hit.collider.GetComponentInParent<CasinoInteractable>()==reveal,"otro collider intercepta Revelar");
        reveal.Activate();timeout=Time.time+4;while(!g.CardsRevealed&&Time.time<timeout)yield return null;
        Require(g.CardsRevealed,"la animacion no confirma las cartas reveladas");
        var cards=FindFirstObjectByType<CasinoCardReveal>();Require(cards!=null&&cards.fronts[0].activeSelf&&cards.fronts[1].activeSelf,"anversos invisibles");
        g.Act(CasinoAction.CashOut);g.ui.CloseInfo();Require(g.counter.cashOut==g.data.representedCash,"contador incorrecto");
        g.Act(CasinoAction.Finish);g.ui.CloseInfo();g.Act(CasinoAction.ExplainCorrect);g.ui.CloseInfo();g.Act(CasinoAction.Finish);g.ui.CloseInfo();Require(g.Completed,"no finaliza Ataque");
        g.ResetScenario(SimMode.Defensa);g.ui.CloseInfo();g.Act(CasinoAction.Ledger);g.ui.CloseInfo();g.Act(CasinoAction.SharedAlert);g.ui.CloseInfo();g.Act(CasinoAction.Report);g.ui.CloseInfo();
        Require(g.Frozen&&g.counter.frozen==g.data.funds,"no congela en Defensa");g.Act(CasinoAction.ToCasino);g.ui.CloseInfo();Require(!g.InCasino,"Defensa permite pasar al casino");
        Debug.Log("PRUEBA V5 COMPLETADA: pistas, portal, fichas, raycast de Revelar, cartas, canje, cierre y bloqueo de Defensa. Revisar apariencia/estereo y rendimiento aparte.");
        running=false;Destroy(owner);
    }
    void OnDestroy(){running=false;}
}
