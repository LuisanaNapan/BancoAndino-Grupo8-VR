using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public bool generationComplete;
    public ScenarioData data;
    public SimMode mode = SimMode.Ataque;
    public UIManager ui;
    public AudioManager audioManager;
    public MoneyCounter counter;
    public AmlAlert aml;
    public ChipAnimator chipAnimator;
    public Transform playerRig, exchangeSpawn, casinoSpawn;
    readonly HashSet<string> evidence = new HashSet<string>();
    readonly HashSet<string> defenses = new HashSet<string>();
    bool converted, inCasino, betComplete, explained;
    bool explaining, finished, bankPlaced, playerPlaced, dealt, revealRequested;
    public bool RevealRequested { get { return revealRequested; } }
    public bool BankPlaced { get { return bankPlaced; } }
    public bool PlayerPlaced { get { return playerPlaced; } }
    public bool Completed { get { return finished; } }
    public bool ExplanationAccepted => explained;
    public string VoiceStatus => narrator == null || narrator.Muted ? "VOZ OFF" : $"VOZ {Mathf.RoundToInt(narrator.Volume * 100)}%";
    public bool CashCollected { get { return counter.cashOut>0; } }
    public bool HasEvidence(string id){return evidence.Contains(id);}
    public bool VisibleAction(CasinoAction a)
    {
        if(a==CasinoAction.PlaceBank||a==CasinoAction.PlacePlayer)return inCasino&&!dealt&&!(bankPlaced&&playerPlaced);
        if(a==CasinoAction.Deal)return inCasino&&bankPlaced&&playerPlaced&&!dealt;
        if(a==CasinoAction.CashOut)return inCasino&&dealt&&!CashCollected;
        if(a==CasinoAction.Finish)return inCasino&&CashCollected&&!ChoosingExplanation&&!finished;
        if(a==CasinoAction.ExplainCorrect||a==CasinoAction.ExplainWrong)return ChoosingExplanation;
        if(a==CasinoAction.Convert)return mode==SimMode.Ataque&&!converted;
        if(a==CasinoAction.Report)return mode==SimMode.Defensa&&!finished;
        return true;
    }
    public bool EnabledAction(CasinoAction a)
    {
        if(a==CasinoAction.PlaceBank)return !chipAnimator.running&&!bankPlaced;
        if(a==CasinoAction.PlacePlayer)return !chipAnimator.running&&!playerPlaced;
        if(a==CasinoAction.Deal)return !revealRequested;
        if(a==CasinoAction.Convert)return evidence.Count==3;
        if(a==CasinoAction.Report)return evidence.Contains("libro")&&evidence.Contains("alerta");
        return true;
    }
    public bool CardsRevealed { get { return dealt; } }
    public bool Frozen { get { return aml!=null&&aml.frozen; } }
    public bool ChoosingExplanation { get { return explaining&&!explained&&!finished&&counter.cashOut>0; } }
    public bool InCasino { get { return inCasino; } }
    public string Objective
    {
        get
        {
            if (finished) return Frozen ? "Portal bloqueado · Compara con Ataque" : "Caso completado · Compara con Defensa";
            if (mode == SimMode.Defensa)
            {
                if (!evidence.Contains("libro")) return "Defensa · Revisa el libro del mostrador";
                if (!evidence.Contains("alerta")) return "Defensa · Revisa la alerta de la terminal";
                return "Defensa · Emite el reporte en la consola";
            }
            if(!evidence.Contains("libro")) return "Ataque · Revisa el libro del mostrador";
            if(!evidence.Contains("cambio")) return "Ataque · Revisa el comprobante de cambio";
            if(!evidence.Contains("alerta")) return "Ataque · Revisa la alerta de la terminal";
            if(!converted) return "Registra el cambio en la consola";
            if(!inCasino) return "Usa el portal para entrar al casino";
            if(chipAnimator.running)return "Observa cómo se colocan las fichas";
            if(!bankPlaced&&!playerPlaced)return "Coloca fichas en Banca o Jugador";
            if(!bankPlaced)return "Completa el grupo de Banca";
            if(!playerPlaced)return "Completa el grupo de Jugador";
            if(revealRequested&&!dealt)return "Observa el giro de las cartas sobre el tapete";
            if(!dealt)return "Revela las cartas desde la consola lateral";
            if(counter.cashOut==0) return "Canjea las fichas en la consola";
            if(explaining&&!explained) return "Elige la explicación en la consola lateral";
            return explained ? "Cierra el caso en la consola" : "Abre Explicar el caso en la consola";
        }
    }
    public bool IsNext(CasinoAction a)
    {
        if(finished) return a==(mode==SimMode.Ataque?CasinoAction.Defense:CasinoAction.Attack);
        if(mode==SimMode.Defensa)
        {
            if(!evidence.Contains("libro"))return a==CasinoAction.Ledger;
            if(!evidence.Contains("alerta"))return a==CasinoAction.SharedAlert;
            return a==CasinoAction.Report;
        }
        if(!evidence.Contains("libro"))return a==CasinoAction.Ledger;
        if(!evidence.Contains("cambio"))return a==CasinoAction.Receipt;
        if(!evidence.Contains("alerta"))return a==CasinoAction.SharedAlert;
        if(!converted)return a==CasinoAction.Convert;
        if(!inCasino)return a==CasinoAction.ToCasino;
        if(chipAnimator.running)return false;
        if(!bankPlaced||!playerPlaced)return (a==CasinoAction.PlaceBank&&!bankPlaced)||(a==CasinoAction.PlacePlayer&&!playerPlaced);
        if(!dealt)return a==CasinoAction.Deal;
        if(counter.cashOut==0)return a==CasinoAction.CashOut;
        if(explaining&&!explained)return a==CasinoAction.ExplainCorrect||a==CasinoAction.ExplainWrong;
        return a==CasinoAction.Finish;
    }
    public int EvidenceCount { get { return evidence.Count; } }
    void Awake() { I = this; }
    public bool ValidateConfiguration(out string issue)
    {
        issue="";
        if(!generationComplete)issue="La creacion del escenario no termino.";
        else if(data==null||ui==null||audioManager==null||counter==null||aml==null)issue="Faltan sistemas principales.";
        else if(playerRig==null||exchangeSpawn==null||casinoSpawn==null)issue="Faltan posiciones del jugador.";
        else if(chipAnimator==null||chipAnimator.chips==null||chipAnimator.chips.Length<2||chipAnimator.bankSpot==null||chipAnimator.playerSpot==null)issue="Faltan fichas o destinos de la mesa.";
        else if(ui.infoRoot==null||ui.infoText==null||ui.view==null||ui.objectiveTexts==null||ui.objectiveTexts.Length!=2||ui.counterTexts==null||ui.counterTexts.Length!=2||ui.modeTexts==null||ui.modeTexts.Length!=2)issue="La interfaz quedo incompleta.";
        else if(audioManager.effects==null||audioManager.ambience==null)issue="Falta configurar el audio.";
        return string.IsNullOrEmpty(issue);
    }
    void Start()
    {
        string issue;
        if(!ValidateConfiguration(out issue))
        {
            Debug.LogError("CASINO V5 INCOMPLETO: "+issue+" Deten Play y ejecuta Casino 8 > Abrir proyecto > Crear escenario. Revisa el aviso de creacion antes de Play.");
            foreach(var pointer in FindObjectsByType<CasinoPointer>(FindObjectsSortMode.None))pointer.enabled=false;
            I=null;enabled=false;return;
        }
        ui.PrepareField(this);
        narrator = gameObject.AddComponent<CasinoNarrator>();
        narrator.Initialize(ui, audioManager);
        CasinoOperationsRoom.Install(this);
        ResetScenario(SimMode.Ataque);
    }
    void OnDestroy() { if (I == this) I = null; }
    public void RegisterEvidence(string id) { evidence.Add(id); ui.Refresh(this); }
    public void ApplyDefense(string id) { defenses.Add(id); ui.Refresh(this); }
    CasinoNarrator narrator;
    void Guide(string id, string speech)
    {
        narrator.Speak(id, speech);
    }

    void Show(string text, string speech = null, string clip = null)
    {
        ui.Show(text);
        ui.Refresh(this);
        if (speech != null) Guide(clip, speech);
    }
    public void ResetScenario(SimMode nextMode)
    {
        mode = nextMode; bankPlaced=false; playerPlaced=false; dealt=false; revealRequested=false; explaining=false; finished=false; evidence.Clear(); defenses.Clear(); converted = false; inCasino = false;
        betComplete = false; explained = false; aml.frozen = false; counter.ResetCounter();
        chipAnimator.ResetChips(); Teleport(exchangeSpawn);
        ui.Read("", "");
        ui.Refresh(this);
        bool defense = mode == SimMode.Defensa;
        narrator.Welcome(
            ("inicio-saludo", "Hola, qué bueno que llegaste. Soy tu compañera del equipo de investigación del Banco Andino. Necesitamos tu ayuda."),
            ("inicio-incidente", "Alguien entró en los sistemas del banco y envió dinero sin autorización."),
            ("inicio-rastro", "Sabemos que una parte pasó por esta casa de cambio. Podría terminar en el casino. Tenemos que averiguar cómo."),
            defense
                ? ("inicio-mision-defensa", "Juntemos las pistas. Así podremos decidir si debemos detener el dinero antes de que llegue al casino.")
                : ("inicio-mision-ataque", "Vamos a reconstruir el recorrido del dinero. Al relacionar las pistas, podremos entender cómo llegó hasta allí."),
            Application.isEditor
                ? ("inicio-control-editor", "Abre el libro con un clic. Para probar la mirada, pulsa G, gira con el botón derecho y mantén la mira hasta completar el círculo.")
                : ("inicio-control-cardboard", "¿Ves el libro sobre el mostrador? Míralo. El puntero de tu mano sigue tu mirada. Mantenlo sobre el libro hasta completar el círculo."),
            ("inicio-acompana", "Vamos paso a paso. Yo te acompañaré."));
    }
    void Teleport(Transform destination)
    {
        TeleportPoint(destination.position);
    }
    void TeleportPoint(Vector3 destination)
    {
        var walker=playerRig.GetComponent<CharacterController>();
        if(walker!=null)walker.enabled=false;
        playerRig.SetPositionAndRotation(destination, Quaternion.identity);
        if(walker!=null)walker.enabled=true;
        var pointer = playerRig.GetComponent<CasinoPointer>();
        if (pointer != null) pointer.ResetView();
    }
    public void Act(CasinoAction action)
    {
        switch (action)
        {
            case CasinoAction.ReplayGuide: narrator.Replay(); return;
            case CasinoAction.MuteNarration: narrator.ToggleMute(); return;
            case CasinoAction.VoiceDown: narrator.AdjustVolume(-.25f); return;
            case CasinoAction.VoiceUp: narrator.AdjustVolume(.25f); return;
        }
        audioManager.SelectAction(action);
        ui.Read("", "");
        switch (action)
        {
            case CasinoAction.InfoNext: ui.NextInfo(); return;
            case CasinoAction.InfoClose: ui.CloseInfo(); return;
            case CasinoAction.Host:
                if (!inCasino)
                {
                    Show("Sobre el caso · Recreación educativa\nInspirada en el incidente de Bangladesh Bank de 2016. Una intrusión dio lugar a pagos fraudulentos. US$81 millones llegaron a cuentas en Filipinas.\nBanco Andino, OR-08 y las cifras del ejercicio son parte de la simulación.",
                        "La recreación comienza después del envío de órdenes fraudulentas. En el banco, la doble autorización y el monitoreo de pagos son controles propuestos. Retoma el objetivo que ves arriba.", "antecedente");
                    return;
                }
                if(inCasino&&dealt){Show("LO QUE ACABAS DE VER\nBanca 7 y Jugador 6: un lado gana y el otro pierde.\nLas apuestas opuestas no garantizan beneficio.\nLo investigado es el origen del dinero y su trazabilidad.");return;}
                Show(inCasino?"ANFITRION / TU TURNO\nElige donde colocar las fichas, revela las cartas y revisa el resultado.\n"+Objective:"ANALISTA / INVESTIGA\nLos documentos son pistas: mira el libro, el comprobante y la terminal.\n"+Objective);return;
            case CasinoAction.PlaceBank:
            case CasinoAction.PlacePlayer:
                if(!inCasino||aml.frozen){Show("MESA NO DISPONIBLE\nCompleta la casa de cambio y cruza el portal.");return;}
                if(chipAnimator.running){Show("ESPERA UN MOMENTO\nLas fichas se estan colocando.");return;}
                bool bank=action==CasinoAction.PlaceBank;
                if(bank?bankPlaced:playerPlaced){Show("FICHAS YA COLOCADAS\nContinua con el siguiente paso.");return;}
                audioManager.Chips();chipAnimator.PlaySide(bank);return;
            case CasinoAction.Deal:
                if(!bankPlaced||!playerPlaced){Show("FALTAN FICHAS\nColoca un grupo en BANCA y otro en JUGADOR.");return;}
                if(revealRequested)return;
                revealRequested=true;ui.Refresh(this);return;
            case CasinoAction.ToConsole:
                TeleportPoint(new Vector3((inCasino?20:0)-2.5f,1.75f,-.75f));return;
            case CasinoAction.Approach:
                TeleportPoint(new Vector3(inCasino?20:0,1.75f,-2.5f)); return;
            case CasinoAction.Overview:
                Teleport(inCasino?casinoSpawn:exchangeSpawn); return;
            case CasinoAction.ToggleMusic:
                audioManager.ToggleMusic(); return;
            case CasinoAction.Attack: ResetScenario(SimMode.Ataque); return;
            case CasinoAction.Defense: ResetScenario(SimMode.Defensa); return;
            case CasinoAction.Restart: ResetScenario(mode); return;
            case CasinoAction.Ledger:
                OpenLedger();
                return;
            case CasinoAction.Receipt:
                Review("cambio", "Comprobante · OR-08",
                    "Entrada: " + MoneyCounter.Short(data.funds) + " en dólares\nSalida: saldo equivalente en pesos del ejercicio\nOR-08 enlaza el registro con el libro. No se aplica una tasa real.",
                    "comprobante", "El comprobante vincula el saldo en dólares con el equivalente en pesos. El origen sigue siendo O R cero ocho.");
                return;
            case CasinoAction.SharedAlert:
                Review("alerta", "Alerta compartida · Monitoreo del banco",
                    "Origen señalado: OR-08\nEl equipo de monitoreo lo vinculó con los pagos investigados. La terminal comparte ese aviso con la casa de cambio.\nCruza el identificador con el libro.",
                    "alerta", "La terminal recibió un aviso del equipo de monitoreo del banco. Señala el mismo origen que debes verificar en el libro.");
                return;
            case CasinoAction.Convert:
                if (aml.frozen) { Show("OPERACION BLOQUEADA\nLos fondos estan congelados. No pueden convertirse."); return; }
                if (mode != SimMode.Ataque || !EnabledAction(action) || converted) return;
                converted = true; counter.stage = "Casa de cambio / pesos";
                RegisterEvidence("cambio");
                narrator.StopWelcome();
                Guide("cambio-registrado", "Registraste el cambio de dólares a pesos. La consola conserva el importe de referencia. Usa el portal a la derecha para seguir los fondos."); return;
            case CasinoAction.Report:
                if (mode != SimMode.Defensa) { Show("ATAQUE / CONTROL AUSENTE EN LA RECREACION\nEn este recorrido el flujo sigue sin bloqueo.\nSelecciona DEFENSA para aplicar el reporte."); return; }
                if (aml.frozen) { EndScenario(); return; }
                long sum; int n;
                bool pattern = evidence.Contains("libro") && StructuringDetector.Detect(data.operations, data.demonstrationThreshold, out sum, out n);
                if (!aml.TryFreeze(mode, pattern, evidence.Contains("alerta"), inCasino))
                { Show("ROS PENDIENTE\nRevisa el LIBRO y la ALERTA COMPARTIDA.\nEl bloqueo debe ocurrir en la casa de cambio."); return; }
                ApplyDefense("ROS"); counter.frozen = data.funds; counter.stage = "Congelado antes del casino";
                audioManager.Alert(); EndScenario(); return;
            case CasinoAction.ToCasino:
                if (aml.frozen) { ui.Read("Portal bloqueado", $"Retenido: {MoneyCounter.Short(counter.frozen)} · Al casino: US$ 0\nLa barrera representa la intervención del ejercicio."); return; }
                if (mode == SimMode.Defensa) { Show("MODO DEFENSA\nEmite el ROS en la casa de cambio antes del casino.\nPara ver el flujo sin controles, elige ATAQUE."); return; }
                if (!converted || evidence.Count < 3) { Show("Antes de cruzar: registra las 3 evidencias\ny pulsa CONVERTIR."); return; }
                inCasino = true; counter.stage = "Casino / fichas"; Teleport(casinoSpawn);
                Guide("entrada-casino", "Ya estás en el casino. Las fichas representan fondos convertidos desde pesos. Mira el círculo de acercamiento o el de la consola para colocar los dos grupos."); return;
            case CasinoAction.ToExchange:
                if(chipAnimator.running){Show("ESPERA\nTermina de colocar las fichas antes de salir.");return;}
                inCasino=false; Teleport(exchangeSpawn); Show("Regresaste a la casa de cambio.\nEl recorrido conserva las evidencias registradas."); return;
            case CasinoAction.Bet:
                Show("MESA DE BACARA / TU TURNO\nSelecciona COLOCAR EN BANCA y COLOCAR EN JUGADOR.\nDespues revela las cartas. Es una escena educativa, no una partida completa.");return;
            case CasinoAction.CashOut:
                if (!betComplete || aml.frozen) { Show("Coloca ambos grupos de fichas y revela las cartas."); return; }
                counter.cashOut = data.representedCash; counter.stage = "Fichas -> efectivo (tramo representado)";
                Show($"Canje · Efectivo representado\n{MoneyCounter.Short(counter.cashOut)} del tramo en caja. El ejercicio fija este importe sin calcularlo a partir de la partida.",
                    "La caja registra un importe didáctico fijo para el tramo en efectivo. El importe es independiente del resultado de las cartas. Selecciona Explicar el caso para revisar qué ocurrió con la trazabilidad.", "canje");return;
            case CasinoAction.ExplainCorrect:
                if (counter.cashOut == 0) { Show("Completa primero la mesa y el canje de fichas."); return; }
                explained = true;
                Guide("explicacion-correcta", "Así es. Los fondos cambiaron de forma y su origen es más difícil de seguir. Selecciona Cerrar caso."); return;
            case CasinoAction.ExplainWrong:
                Guide("explicacion-revisar", "Banca ganó y Jugador perdió en la mano preparada. Piensa en lo que cuesta identificar de dónde vino el dinero y elige otra vez."); return;
            case CasinoAction.Finish:
                if (mode == SimMode.Ataque && counter.cashOut > 0 && !explained)
                { explaining=true;ui.Refresh(this);Guide("explicacion-pregunta", "¿Qué efecto ilustra el recorrido? Elige la respuesta en la consola.");return; }
                EndScenario(); return;
            case CasinoAction.Credits:
                Show("CREDITOS / ESCENARIO 08\nEscenario: guia Banco Andino del curso.\nModelos 3D, musica y sonidos originales de esta entrega.\nCardboard: Google, Apache 2.0. Unity/URP: sus licencias.\nImplementacion asistida por ChatGPT/Codex.\nEquipo: completar nombres, roles y fuentes en LEEME."); return;
        }
    }
    void OpenLedger()
    {
        bool pattern = StructuringDetector.Detect(data.operations, data.demonstrationThreshold, out long total, out int count);
        var rows = new List<string>();
        for (int i = 0; i < data.operations.Length; i += 2)
        {
            string row = $"OR-08   {i + 1:00}   {MoneyCounter.Short(data.operations[i])}";
            if (i + 1 < data.operations.Length) row += $"     |     {i + 2:00}   {MoneyCounter.Short(data.operations[i + 1])}";
            rows.Add(row);
        }
        rows.Add($"{count} movimientos del mismo origen = {MoneyCounter.Short(total)}");
        rows.Add($"Umbral del ejercicio: {MoneyCounter.Short(data.demonstrationThreshold)} · {(pattern ? "alcanzado" : "sin coincidencia")}");
        Review("libro", "Libro de operaciones · OR-08", string.Join("\n", rows),
            "libro", "Estos movimientos comparten el origen O R cero ocho. El detector los suma y compara el total con el umbral del ejercicio.");
    }

    void Review(string key, string title, string body, string clip, string speech)
    {
        bool first = !HasEvidence(key);
        RegisterEvidence(key);
        ui.Read(title, body);
        if (!first) return;
        string next = Objective;
        int modeSeparator = next.IndexOf(" · ");
        if (modeSeparator >= 0) next = next.Substring(modeSeparator + 3);
        Guide(clip + "-" + NextEvidenceClip(), speech + " " + next + ".");
    }

    string NextEvidenceClip()
    {
        if (!HasEvidence("libro")) return "paso-libro";
        if (mode == SimMode.Defensa) return HasEvidence("alerta") ? "paso-reporte" : "paso-alerta";
        if (!HasEvidence("cambio")) return "paso-comprobante";
        return HasEvidence("alerta") ? "paso-cambio" : "paso-alerta";
    }
    public void SidePlaced(bool bank)
    {
        if(bank)bankPlaced=true;else playerPlaced=true;
        ui.Refresh(this);
        if (bankPlaced && playerPlaced)
            Guide("fichas-listas", "Colocaste fichas en ambos lados. La simulación representa resultados opuestos en una misma mano. Selecciona Revelar cartas en la consola.");
        else
            Guide(bank ? "fichas-banca" : "fichas-jugador",
                bank ? "El primer grupo está en Banca. Coloca el segundo grupo en Jugador." : "El primer grupo está en Jugador. Coloca el segundo grupo en Banca.");
    }
    public void RevealFinished()
    {
        dealt=true;betComplete=true;counter.stage="BANCA 7 / JUGADOR 6";ui.Refresh(this);
        Guide("cartas", "El resultado está preparado para el ejercicio. Banca suma siete y gana. Jugador suma seis y pierde. Selecciona Canjear fichas para ver el tramo representado.");
    }
    public void EndScenario()
    {
        if (mode == SimMode.Defensa && !aml.frozen) { Show("Falta emitir el ROS para terminar el modo defensa."); return; }
        if (mode == SimMode.Ataque && counter.cashOut == 0) { Show("Completa: evidencias -> convertir -> portal\n-> mesa -> canjear fichas -> cerrar caso."); return; }
        finished=true;
        narrator.StopWelcome();
        StructuringDetector.Detect(data.operations, data.demonstrationThreshold, out long reportTotal, out int reportCount);
        string conclusion = mode == SimMode.Defensa
            ? $"Intervención simulada\nPatrón OR-08: {reportCount} movimientos, {MoneyCounter.Short(reportTotal)}.\nAlerta compartida verificada. Fondos aún en la casa de cambio.\nRetención: {MoneyCounter.Short(counter.frozen)} · Al casino: US$ 0 · Portal bloqueado.\nEl ejercicio vincula el reporte y la retención."
            : $"Trazabilidad del caso\nRuta analizada: {MoneyCounter.Short(data.funds)}.\nEfectivo representado: {MoneyCounter.Short(counter.cashOut)}, un importe fijo del ejercicio.\nRelacionar los registros permite seguir el origen.";
        Show(conclusion, mode == SimMode.Defensa
            ? "Verificamos tres cosas. Los movimientos del libro forman un patrón. La alerta coincide con el origen. Y los fondos siguen en la casa de cambio. El reporte activa la retención simulada del saldo completo. Mira el portal bloqueado."
            : "Reconstruiste la ruta completa. Cambiar de forma dificulta seguir el origen del dinero. Selecciona Defensa para investigar cómo intervenir antes del casino.",
            mode == SimMode.Defensa ? "cierre-defensa" : "cierre-ataque");
        ui.Result(conclusion);
        audioManager.Ending();
    }
}
