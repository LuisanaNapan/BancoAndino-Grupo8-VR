using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CasinoRuntimeCheck : MonoBehaviour
{
    static bool running;
    public static void Begin(bool capture = false)
    {
        if(!Application.isPlaying){Debug.LogWarning("Pulsa Play antes de iniciar la prueba. La prueba reinicia el recorrido.");return;}
        if(running)return;
        string directory = null;
        if (capture)
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas_QA",
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")));
            Directory.CreateDirectory(directory);
            Debug.Log("Capturas solicitadas en " + directory);
        }
        running = true;
        var go = new GameObject("Comprobacion_Casino");
        go.AddComponent<CasinoRuntimeCheck>().StartCoroutine(Guard(go, directory));
    }
    static IEnumerator Guard(GameObject owner, string directory)
    {
        var test=Run(owner, directory);
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
    static IEnumerator Capture(string directory, string name)
    {
        if (directory == null) yield break;
        yield return null;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
        yield return new WaitForSecondsRealtime(.4f);
    }

    static IEnumerator Run(GameObject owner, string directory)
    {
        yield return null;var g=GameManager.I;
        if(g==null){Debug.LogError("No esta abierta la escena Casino_VR.");running=false;Destroy(owner);yield break;}
        g.ResetScenario(SimMode.Ataque);
        yield return Capture(directory, "01_inicio");
        g.ui.CloseInfo();
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
        yield return Capture(directory, "02_cartas");
        var cards=FindFirstObjectByType<CasinoCardReveal>();Require(cards!=null&&cards.fronts[0].activeSelf&&cards.fronts[1].activeSelf,"anversos invisibles");
        g.Act(CasinoAction.CashOut);g.ui.CloseInfo();Require(g.counter.cashOut==g.data.representedCash,"contador incorrecto");
        g.Act(CasinoAction.Finish);g.ui.CloseInfo();g.Act(CasinoAction.ExplainCorrect);g.ui.CloseInfo();g.Act(CasinoAction.Finish);g.ui.CloseInfo();Require(g.Completed,"no finaliza Ataque");
        g.ResetScenario(SimMode.Defensa);
        yield return Capture(directory, "03_guia_defensa");
        g.ui.CloseInfo();
        g.Act(CasinoAction.Report);
        g.ui.CloseInfo();
        Require(!g.Frozen, "bloquea antes de revisar las evidencias");

        g.Act(CasinoAction.Ledger);
        g.ui.CloseInfo();
        g.Act(CasinoAction.Report);
        g.ui.CloseInfo();
        Require(!g.Frozen, "bloquea sin revisar la alerta");

        g.Act(CasinoAction.SharedAlert);
        yield return Capture(directory, "04_alerta");
        g.ui.CloseInfo();
        g.Act(CasinoAction.Report);
        Require(g.Frozen && g.counter.frozen == g.data.funds, "no congela en Defensa");
        yield return Capture(directory, "05_reporte");
        g.ui.CloseInfo();
        g.Act(CasinoAction.ToCasino);
        Require(!g.InCasino, "Defensa permite pasar al casino");
        yield return Capture(directory, "06_portal_bloqueado");
        g.ui.CloseInfo();
        Debug.Log("PRUEBA COMPLETADA: pistas, acceso, fichas, cartas, canje y condiciones de Defensa.");
        running=false;Destroy(owner);
    }
    void OnDestroy(){running=false;}
}
