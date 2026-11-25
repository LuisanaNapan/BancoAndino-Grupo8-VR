using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CasinoRuntimeCheck : MonoBehaviour
{
    static bool running;
    public static void Begin(bool capture = false, string captureRoot = "Capturas_QA")
    {
        if(!Application.isPlaying){Debug.LogWarning("Pulsa Play antes de iniciar la prueba. La prueba reinicia el recorrido.");return;}
        if(running)return;
        string directory = null;
        if (capture)
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", captureRoot,
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
        Require(!g.ui.InfoOpen && g.ui.Reading.Length == 0
            && g.ui.Subtitle.StartsWith("Hola, qué bueno que llegaste") && g.ui.Subtitle.Contains("Banco Andino"),
            "la bienvenida debe empezar con un subtítulo breve y sin cartel");
        string initialVoice = g.VoiceStatus;
        g.Act(CasinoAction.MuteNarration);
        Require(g.VoiceStatus != initialVoice, "el botón de silencio no cambia el estado de la voz");
        g.Act(CasinoAction.MuteNarration);
        Require(g.VoiceStatus == initialVoice, "el botón de silencio no restaura el estado anterior");
        bool volumeTest = g.VoiceStatus != "VOZ OFF" && g.VoiceStatus != "VOZ 0%";
        if (volumeTest)
        {
            g.Act(CasinoAction.VoiceDown);
            Require(g.VoiceStatus != initialVoice, "el control de volumen no reduce la voz");
            g.Act(CasinoAction.VoiceUp);
            Require(g.VoiceStatus == initialVoice, "el nivel de voz no se restaura");
        }
        g.Act(CasinoAction.ReplayGuide);
        Require(g.ui.Subtitle.Contains("Banco Andino"), "no repite la bienvenida");
        yield return Capture(directory, "01_inicio");
        g.ui.CloseInfo();
        g.Act(CasinoAction.ToCasino);g.ui.CloseInfo();Require(!g.InCasino,"portal abierto sin evidencias");
        var pointer = g.playerRig.GetComponent<CasinoPointer>();
        var camera = g.ui.view.transform;
        Require(camera.Find("Mano izquierda") != null && camera.Find("Mano derecha") != null, "faltan las manos de primera persona");
        CasinoInteractable ledger = null;
        foreach (var target in FindObjectsByType<CasinoInteractable>())
            if (target.action == CasinoAction.Ledger) ledger = target;
        Require(ledger != null, "falta el libro");
        var hitPoint = ledger.GetComponent<Collider>().bounds.center;
        var originalRotation = camera.localRotation;
        camera.rotation = Quaternion.LookRotation(hitPoint - camera.position);
        Physics.SyncTransforms();
        pointer.enabled = false;
        bool progressSeen = false;
        bool confirmedSeen = false;
        float elapsed = 0f;
        while (!g.HasEvidence("libro") && elapsed < pointer.dwellSeconds + 1f)
        {
            pointer.Select(new Ray(camera.position, camera.forward), true, false);
            progressSeen |= pointer.FocusedAction == CasinoAction.Ledger && pointer.DwellProgress > .15f;
            confirmedSeen |= pointer.Confirming;
            elapsed += Time.deltaTime;
            yield return null;
        }
        pointer.enabled = true;
        Require(progressSeen && confirmedSeen && g.HasEvidence("libro"), "la mirada no enfoca y activa el libro");
        Require(g.ui.Reading.Contains(MoneyCounter.Short(g.data.demonstrationThreshold))
            && g.ui.Reading.Contains(MoneyCounter.Short(g.data.funds)), "faltan total y umbral en el libro");
        string ledgerReading = g.ui.Reading;
        g.Act(CasinoAction.Ledger);
        Require(g.ui.Reading == ledgerReading && !g.ui.InfoOpen, "la relectura debe mostrar la evidencia sin modal");
        yield return Capture(directory, "02_libro");
        camera.localRotation = originalRotation;
        g.Act(CasinoAction.Receipt);
        Require(g.ui.Reading.Contains("pesos"), "falta la moneda de salida del comprobante");
        g.Act(CasinoAction.SharedAlert);
        Require(g.ui.Reading.Contains("OR-08"), "falta el origen de la alerta");
        Require(g.EvidenceCount==3,"no registra las tres pistas");g.Act(CasinoAction.Convert);g.ui.CloseInfo();g.Act(CasinoAction.ToCasino);g.ui.CloseInfo();
        Require(g.InCasino && Vector3.Distance(g.playerRig.position, g.casinoSpawn.position) < .05f,"no permite entrar tras completar los requisitos");
        Require(g.ui.Subtitle.Contains("Ya estás en el casino"), "la guía del casino debe reemplazar la bienvenida al cambiar de tramo");
        yield return Capture(directory, "03_entrada_casino");
        g.Act(CasinoAction.ToConsole);
        g.Act(CasinoAction.PlaceBank);
        float timeout=Time.time+8;while(g.chipAnimator.running&&Time.time<timeout)yield return null;
        Require(g.BankPlaced,"no termina la colocacion en Banca");g.Act(CasinoAction.PlacePlayer);
        timeout=Time.time+8;while(g.chipAnimator.running&&Time.time<timeout)yield return null;
        Require(g.PlayerPlaced,"no termina la colocacion en Jugador");yield return null;
        // Use the real physics pick path to verify that another collider does not intercept Reveal.
        var controls=FindObjectsByType<CasinoInteractable>();CasinoInteractable reveal=null;
        foreach(var c in controls)if(c.action==CasinoAction.Deal)reveal=c;
        Require(reveal!=null,"no aparece el control Revelar");Physics.SyncTransforms();
        var view=g.ui.view;Vector3 delta=reveal.GetComponent<Collider>().bounds.center-view.transform.position;RaycastHit hit;
        Require(Physics.Raycast(view.transform.position,delta.normalized,out hit,delta.magnitude+.2f),"la seleccion no llega a Revelar");
        Require(hit.collider.GetComponentInParent<CasinoInteractable>()==reveal,"otro collider intercepta Revelar");
        reveal.Activate();timeout=Time.time+4;while(!g.CardsRevealed&&Time.time<timeout)yield return null;
        Require(g.CardsRevealed,"la animacion no confirma las cartas reveladas");
        var cards=FindAnyObjectByType<CasinoCardReveal>();Require(cards!=null&&cards.fronts[0].activeSelf&&cards.fronts[1].activeSelf,"anversos invisibles");
        Vector3 table = (cards.cards[0].position + cards.cards[1].position) * .5f;
        camera.rotation = Quaternion.LookRotation(table - camera.position);
        yield return Capture(directory, "04_cartas");
        g.Act(CasinoAction.CashOut);g.ui.CloseInfo();Require(g.counter.cashOut==g.data.representedCash,"contador incorrecto");
        yield return Capture(directory, "05_canje");
        g.Act(CasinoAction.Finish);g.ui.CloseInfo();g.Act(CasinoAction.ExplainCorrect);g.ui.CloseInfo();g.Act(CasinoAction.Finish);g.ui.CloseInfo();Require(g.Completed,"no finaliza Ataque");
        g.ResetScenario(SimMode.Defensa);
        Require(g.Objective.Contains("Defensa") && g.ui.Subtitle.Contains("Banco Andino"), "no inicia la guía de Defensa");
        yield return Capture(directory, "06_guia_defensa");
        g.Act(CasinoAction.Host);
        Require(g.ui.Reading.Contains("Bangladesh Bank"), "falta la referencia histórica en Sobre el caso");
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
        yield return Capture(directory, "07_alerta");
        g.ui.CloseInfo();
        g.Act(CasinoAction.Report);
        Require(g.Frozen && g.counter.frozen == g.data.funds, "no congela en Defensa");
        Require(g.ui.Subtitle.Contains("Verificamos tres cosas"), "la voz debe explicar la intervención en cuanto se aplica");
        Require(g.ui.Reading.Contains("Portal bloqueado") && g.ui.Reading.Contains("US$ 0"), "no explica qué bloqueó la intervención");
        yield return Capture(directory, "08_reporte");
        g.ui.CloseInfo();
        g.Act(CasinoAction.ToCasino);
        Require(!g.InCasino, "Defensa permite pasar al casino");
        camera.rotation = Quaternion.LookRotation(new Vector3(4.4f, 1.45f, 2.17f) - camera.position);
        yield return Capture(directory, "09_portal_bloqueado");
        g.ui.CloseInfo();
        Debug.Log("PRUEBA COMPLETADA: introducciones, subtítulos, controles de voz, manos, foco y selección por mirada, evidencias, teletransporte, fichas, cartas, canje y bloqueo.");
        running=false;Destroy(owner);
    }
    void OnDestroy(){running=false;}
}
