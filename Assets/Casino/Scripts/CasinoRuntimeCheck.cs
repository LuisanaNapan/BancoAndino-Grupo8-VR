using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CasinoRuntimeCheck : MonoBehaviour
{
    static bool running;
    CasinoPointer pointer;
    bool restoreGaze, restorePointer;

    public static void Begin(bool capture = false, string captureRoot = "Capturas_QA")
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Abre Play para ejecutar la prueba del escenario.");
            return;
        }
        if (running) return;
        string directory = null;
        if (capture)
        {
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", captureRoot,
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")));
            Directory.CreateDirectory(directory);
            Debug.Log("Capturas solicitadas en " + directory);
        }
        running = true;
        var check = new GameObject("Comprobación Cardboard").AddComponent<CasinoRuntimeCheck>();
        check.StartCoroutine(check.Guard(directory));
    }

    IEnumerator Guard(string directory)
    {
        var test = Run(directory);
        while (true)
        {
            bool more;
            try { more = test.MoveNext(); }
            catch (Exception error) { Debug.LogException(error); break; }
            if (!more) break;
            // Flatten nested steps so failures are reported here and the pointer is always restored.
            if (test.Current is IEnumerator step)
            {
                bool failed = false;
                while (true)
                {
                    try { more = step.MoveNext(); }
                    catch (Exception error) { Debug.LogException(error); failed = true; break; }
                    if (!more) break;
                    yield return step.Current;
                }
                if (failed) break;
            }
            else yield return test.Current;
        }
        Destroy(gameObject);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Prueba Cardboard: " + message);
    }

    static IEnumerator Capture(string directory, string name)
    {
        if (directory == null) yield break;
        yield return null;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
        yield return new WaitForSecondsRealtime(.35f);
    }

    Ray Face(Vector3 direction)
    {
        var view = pointer.view.transform;
        view.rotation = Quaternion.LookRotation(direction, Vector3.up);
        return new Ray(view.position, view.forward);
    }

    void LookAway()
    {
        pointer.Select(Face(new Vector3(0f, 1f, .1f)), false, false);
        Require(pointer.FocusedTarget == null && pointer.DwellProgress == 0f, "apartar la mirada debe cancelar el foco");
    }

    Ray Aim(CasinoAction action, string named = null)
    {
        Physics.SyncTransforms();
        string obstruction = "ningún collider activo";
        foreach (var candidate in FindObjectsByType<CasinoInteractable>())
        {
            if (candidate.action != action || (named != null && candidate.name != named)) continue;
            var collider = candidate.GetComponent<Collider>();
            if (collider == null || !collider.enabled) continue;
            var bounds = collider.bounds;
            foreach (var offset in new[] { Vector3.zero, Vector3.up * .6f, Vector3.down * .6f, Vector3.left * .6f, Vector3.right * .6f })
            {
                var ray = Face(bounds.center + Vector3.Scale(offset, bounds.extents) - pointer.view.transform.position);
                pointer.Select(ray, false, false);
                if (pointer.FocusedTarget == candidate) return ray;
                if (Physics.Raycast(ray, out var hit, pointer.maxDistance)) obstruction = hit.collider.name;
            }
        }
        throw new InvalidOperationException($"Prueba Cardboard: {action} ({named ?? "cualquier control"}) no es seleccionable desde {pointer.transform.position}. Primer impacto: {obstruction}.");
    }

    static float StepTime => Mathf.Min(Time.unscaledDeltaTime, .12f);

    static IEnumerator WaitForAction(Func<bool> condition, string failure, float seconds = 8f)
    {
        for (float elapsed = 0f; elapsed < seconds && !condition(); elapsed += StepTime)
            yield return null;
        Require(condition(), failure);
    }

    IEnumerator Gaze(CasinoAction action, string named = null)
    {
        LookAway();
        yield return null;
        Ray ray = Aim(action, named);
        int before = pointer.ActivationCount;
        float elapsed = 0f;
        while (pointer.ActivationCount == before && elapsed < pointer.dwellSeconds + 1f)
        {
            pointer.Select(ray, true, false);
            elapsed += StepTime;
            if (pointer.ActivationCount == before) yield return null;
        }
        Require(pointer.ActivationCount == before + 1 && pointer.Confirming, $"{action} no se confirmó al completar el círculo");
        float waiting = 0f;
        while (GameManager.I.Teleporting && waiting < 2f)
        {
            waiting += StepTime;
            yield return null;
        }
        Require(!GameManager.I.Teleporting, $"no terminó el fundido de {action}");
        yield return null;
    }

    IEnumerator Practice(GameManager game, string directory)
    {
        Require(game.Tutorial == CasinoTutorialStep.SelectBook, "debe elegir el recorrido antes del tutorial");
        var ray = Aim(CasinoAction.Ledger);
        var beam = pointer.transform.Find("Haz de selección").GetComponent<LineRenderer>();
        Require(beam.enabled && Vector3.Distance(beam.GetPosition(1), pointer.AimPoint) < .001f,
            "la mano y la selección deben señalar el mismo punto");
        for (float elapsed = 0f; elapsed < .4f; elapsed += StepTime)
        {
            pointer.Select(ray, true, false);
            yield return null;
        }
        Require(pointer.DwellProgress > 0f && game.EvidenceCount == 0, "el indicador debe avanzar sin activar el libro antes de tiempo");
        LookAway();
        Require(game.Tutorial == CasinoTutorialStep.SelectBook, "mirar brevemente no debe avanzar el tutorial");

        ray = Aim(CasinoAction.Ledger);
        int before = pointer.ActivationCount;
        for (float elapsed = 0f; elapsed < pointer.dwellSeconds + .7f; elapsed += StepTime)
        {
            pointer.Select(ray, true, false);
            yield return null;
        }
        Require(pointer.ActivationCount == before + 1 && game.Tutorial == CasinoTutorialStep.CloseBook
            && game.ui.Reading.Contains(MoneyCounter.Short(game.data.demonstrationThreshold)),
            "el libro debe abrirse una vez y conservarse abierto mientras se mantiene la mirada");
        if (directory != null)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02_libro.png"));
            yield return new WaitForSecondsRealtime(.35f);
        }
    }

    IEnumerator Run(string directory)
    {
        yield return null;
        var game = GameManager.I;
        Require(game != null, "abre Casino_VR antes de iniciar la prueba");
        pointer = game.playerRig.GetComponent<CasinoPointer>();
        restoreGaze = pointer.UsesGaze;
        restorePointer = pointer.enabled;
        pointer.SetEditorGaze(true);
        pointer.enabled = false;
        game.StartAtModePicker();
        Require(game.ui.view.transform.Find("Mano derecha") != null, "falta la mano del jugador");
        // Both choices must be visible from the initial spawn.
        Aim(CasinoAction.Defense, "Modo_Defensa(Clone)");
        Aim(CasinoAction.Attack, "Modo_Ataque(Clone)");
        pointer.view.transform.localRotation = Quaternion.identity;
        pointer.Select(new Ray(pointer.view.transform.position, pointer.view.transform.forward), false, false);
        yield return Capture(directory, "00_elegir_recorrido");
        yield return Gaze(CasinoAction.Attack, "Modo_Ataque(Clone)");
        Require(game.mode == SimMode.Ataque && game.ui.Reading.Length == 0
            && game.ui.Subtitle.StartsWith("Hola, qué bueno que llegaste"), "falta la bienvenida sin cartel");
        yield return Capture(directory, "01_bienvenida");
        yield return Gaze(CasinoAction.ToCasino);
        Require(!game.InCasino && game.EvidenceCount == 0 && game.Tutorial == CasinoTutorialStep.SelectBook,
            "el portal debe pedir que se complete la práctica");
        yield return WaitForAction(() => game.ReadyForBook, "no aparece la instrucción del libro", 60f);
        Require(game.EvidenceCount == 0 && game.Tutorial == CasinoTutorialStep.SelectBook,
            "terminar la explicación no debe seleccionar el libro por el jugador");
        Require(game.ui.Subtitle.Contains("tu mano sigue tu mirada"),
            "con mirada activa se debe enseñar el control de Cardboard");

        yield return Practice(game, directory);
        yield return Gaze(CasinoAction.Ledger);
        Require(game.ui.Reading.Length == 0 && game.Tutorial == CasinoTutorialStep.Move, "volver al libro debe cerrarlo");
        yield return Gaze(CasinoAction.Approach);
        Require(game.Tutorial == CasinoTutorialStep.Complete && game.playerRig.position.z > game.exchangeSpawn.position.z + 1f,
            "el tutorial debe esperar a ACERCARSE y mover al jugador");
        yield return Gaze(CasinoAction.Ledger);
        Require(game.ui.Reading.Length > 0 && game.EvidenceCount == 1, "no se puede volver a leer el libro");
        yield return Gaze(CasinoAction.Ledger);
        Require(game.ui.Reading.Length == 0, "no se puede volver a cerrar el libro");
        yield return Gaze(CasinoAction.Receipt);
        Require(game.ui.Reading.Contains("pesos"), "el comprobante no explica el cambio");
        yield return Gaze(CasinoAction.Receipt);
        Require(game.ui.Reading.Length == 0, "el comprobante no se puede cerrar");
        yield return Gaze(CasinoAction.SharedAlert);
        Require(game.ui.Reading.Contains("OR-08"), "la alerta no muestra el vínculo con OR-08");
        yield return Capture(directory, "03_alerta");
        yield return Gaze(CasinoAction.ToCasino);
        Require(!game.InCasino && game.EvidenceCount == 3, "el portal deja avanzar antes de registrar el cambio");
        yield return Gaze(CasinoAction.ToConsole);
        yield return Gaze(CasinoAction.Convert);
        Require(game.counter.stage.Contains("pesos"), "no se registró el cambio de moneda");
        yield return Gaze(CasinoAction.Overview);
        yield return Gaze(CasinoAction.ToCasino);
        Require(game.InCasino && Vector3.Distance(game.playerRig.position, game.casinoSpawn.position) < .1f, "no llega al casino");
        yield return Capture(directory, "04_entrada_casino");
        yield return Gaze(CasinoAction.Approach);
        Require(game.playerRig.position.z > game.casinoSpawn.position.z + 1f && game.EnabledAction(CasinoAction.PlaceBank),
            "el punto ACERCARSE no habilita la mesa del casino");
        yield return Gaze(CasinoAction.ToConsole);
        yield return Gaze(CasinoAction.PlaceBank);
        yield return WaitForAction(() => !game.chipAnimator.running, "no terminó la animación de Banca");
        Require(game.BankPlaced && !game.PlayerPlaced && !game.VisibleAction(CasinoAction.Deal), "revelar debe esperar los dos grupos de fichas");
        yield return Gaze(CasinoAction.PlacePlayer);
        yield return WaitForAction(() => !game.chipAnimator.running, "no terminó la animación de Jugador");
        Require(game.PlayerPlaced, "no se colocó el segundo grupo de fichas");
        yield return Gaze(CasinoAction.Deal);
        yield return WaitForAction(() => game.CardsRevealed, "no terminó la revelación de cartas");
        var cards = FindAnyObjectByType<CasinoCardReveal>();
        Require(cards.fronts[0].activeSelf && cards.fronts[1].activeSelf, "los anversos quedaron ocultos");
        Face((cards.cards[0].position + cards.cards[1].position) * .5f - pointer.view.transform.position);
        yield return Capture(directory, "05_cartas");
        yield return Gaze(CasinoAction.CashOut);
        Require(game.counter.cashOut == game.data.representedCash, "el canje no registró el importe didáctico");
        yield return Capture(directory, "06_canje");
        yield return Gaze(CasinoAction.Finish);
        yield return Gaze(CasinoAction.ExplainWrong);
        Require(!game.ExplanationAccepted && game.ChoosingExplanation, "la opción incorrecta terminó el ejercicio");
        yield return Gaze(CasinoAction.ExplainCorrect);
        yield return Gaze(CasinoAction.Finish);
        Require(game.Completed, "el cierre de Ataque no quedó registrado");
        yield return Gaze(CasinoAction.Defense, "Comparar");
        Require(game.mode == SimMode.Defensa && game.EvidenceCount == 0 && !game.InCasino && game.counter.cashOut == 0
            && game.Tutorial == CasinoTutorialStep.SelectBook, "el cambio a Defensa conservó estado de Ataque");
        Require(!game.EnabledAction(CasinoAction.Report), "el reporte aparece habilitado sin evidencias");
        yield return WaitForAction(() => game.ReadyForBook, "Defensa no habilita la práctica del libro", 60f);
        yield return Gaze(CasinoAction.Ledger);
        yield return Gaze(CasinoAction.Ledger);
        yield return Gaze(CasinoAction.Approach);
        Require(!game.EnabledAction(CasinoAction.Report), "se habilitó el reporte antes de ver la alerta");
        yield return Gaze(CasinoAction.ToCasino);
        Require(!game.InCasino && !game.Frozen, "se permitió entrar al casino en Defensa");
        yield return Gaze(CasinoAction.SharedAlert);
        Require(game.EnabledAction(CasinoAction.Report), "el reporte no se habilitó tras libro y alerta");
        yield return Gaze(CasinoAction.ToConsole);
        yield return Gaze(CasinoAction.Report);
        Require(game.Frozen && game.counter.frozen == game.data.funds && game.ui.Reading.Contains("Portal bloqueado"),
            "el reporte no explica o no aplica la intervención");
        yield return Capture(directory, "07_reporte");
        yield return Gaze(CasinoAction.Overview);
        yield return Gaze(CasinoAction.ToCasino);
        Require(!game.InCasino && FindAnyObjectByType<CasinoFreezeVisual>().barrier.activeSelf,
            "el portal permite avanzar o la barrera no aparece tras el reporte");
        yield return Capture(directory, "08_portal_bloqueado");

        yield return Gaze(CasinoAction.Restart);
        Require(game.mode == SimMode.Defensa && !game.Frozen && game.EvidenceCount == 0
            && game.ui.Reading.Length == 0 && game.Tutorial == CasinoTutorialStep.SelectBook, "reiniciar no limpia Defensa");
        yield return Gaze(CasinoAction.ReplayGuide);
        Require(game.ui.Subtitle.Contains("Banco Andino"), "REPETIR GUÍA no recupera el subtítulo");
        string voice = game.VoiceStatus;
        yield return Gaze(CasinoAction.MuteNarration);
        Require(voice != game.VoiceStatus, "el control de silencio no cambia el estado de la voz");
        yield return Gaze(CasinoAction.MuteNarration);
        Require(voice == game.VoiceStatus, "no se recuperó el estado anterior de la voz");
        if (voice != "VOZ OFF" && voice != "VOZ 0%")
        {
            yield return Gaze(CasinoAction.VoiceDown);
            Require(voice != game.VoiceStatus, "el volumen no disminuyó");
            yield return Gaze(CasinoAction.VoiceUp);
            Require(voice == game.VoiceStatus, "el volumen no volvió al nivel anterior");
        }
        yield return Gaze(CasinoAction.Attack);
        Require(game.mode == SimMode.Ataque && game.EvidenceCount == 0, "no se pudo volver a Ataque desde el menú");
        Debug.Log("Prueba Cardboard completada con raycast y permanencia: inicio, tutorial, rotura de foco, acciones, documentos, teletransporte, recorridos y voz.");
    }

    void OnDestroy()
    {
        running = false;
        if (pointer == null) return;
        pointer.SetEditorGaze(restoreGaze);
        pointer.enabled = restorePointer;
    }
}
