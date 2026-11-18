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
            if (finished) return "CASO COMPLETADO / Prueba el otro modo en la estacion izquierda";
            if (mode == SimMode.Defensa)
            {
                if (!evidence.Contains("libro")) return "DEFENSA 1/3 / Revisa el LIBRO del mostrador";
                if (!evidence.Contains("alerta")) return "DEFENSA 2/3 / Revisa la ALERTA de la terminal";
                return "DEFENSA 3/3 / EMITIR ROS: detener el dinero antes del casino";
            }
            if(evidence.Count<3) return "PASO 1/6 / Revisa los 3 objetos del mostrador ("+evidence.Count+"/3)";
            if(!converted) return "PASO 2/6 / REGISTRAR CAMBIO: consola de analisis";
            if(!inCasino) return "PASO 3/6 / Mira el portal IR AL CASINO, a la derecha";
            if(chipAnimator.running)return "MESA / Observa como se colocan tus fichas";
            if(!bankPlaced)return "Elige BANCA o JUGADOR en la consola";
            if(!playerPlaced)return "Completa el segundo grupo de fichas";
            if(revealRequested&&!dealt)return "Observa el giro de las cartas sobre el tapete";
            if(!dealt)return "Revela las cartas desde la consola lateral";
            if(counter.cashOut==0) return "PASO 5/6 / Activa CANJEAR FICHAS";
            if(explaining&&!explained) return "PASO 6/6 / Elige la explicacion en la consola lateral";
            return "PASO 6/6 / EXPLICAR EL CASO: consola lateral";
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
        if(evidence.Count<3)return (a==CasinoAction.Ledger&&!evidence.Contains("libro"))||(a==CasinoAction.Receipt&&!evidence.Contains("cambio"))||(a==CasinoAction.SharedAlert&&!evidence.Contains("alerta"));
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
        ResetScenario(SimMode.Ataque);
    }
    void OnDestroy() { if (I == this) I = null; }
    public void RegisterEvidence(string id) { evidence.Add(id); ui.Refresh(this); }
    public void ApplyDefense(string id) { defenses.Add(id); ui.Refresh(this); }
    CasinoNarrator narrator;
    string lastGuide;

    void Guide(string speech)
    {
        if (narrator == null) narrator = gameObject.AddComponent<CasinoNarrator>();
        lastGuide = speech;
        narrator.Speak(speech);
    }

    void Show(string text, string speech = null)
    {
        ui.Show(text);
        ui.Refresh(this);
        if (speech != null) Guide(speech);
    }
    public void ResetScenario(SimMode nextMode)
    {
        mode = nextMode; bankPlaced=false; playerPlaced=false; dealt=false; revealRequested=false; explaining=false; finished=false; evidence.Clear(); defenses.Clear(); converted = false; inCasino = false;
        betComplete = false; explained = false; aml.frozen = false; counter.ResetCounter();
        chipAnimator.ResetChips(); Teleport(exchangeSpawn);
        ui.Result("CIERRE DEL CASO\nCompleta el recorrido para comparar los resultados.");
        if (mode == SimMode.Defensa)
        {
            Show("DEFENSA / PASO 1 DE 3\nEres el analista de ciberseguridad.\nCierra esta tarjeta y selecciona LIBRO en el mostrador.\nEn visor, mantén la mirada sobre el objeto.",
                "Eres el analista. Cierra esta tarjeta y selecciona el libro del mostrador. Tu primera tarea es relacionar los movimientos del mismo origen.");
            return;
        }
        Show("CENTRO DE CIBERSEGURIDAD / CASO 08\nEl dinero ya salió del banco mediante órdenes fraudulentas.\nCierra esta tarjeta y abre LIBRO para seguir el dinero en Ataque.\nPara investigar y detenerlo, elige DEFENSA en el panel izquierdo o pulsa 2 en PC.", "Estás en el centro de ciberseguridad. El caso empieza después de unas transferencias fraudulentas. Si eres el analista, selecciona Defensa en el panel izquierdo. Para seguir la ruta en Ataque, cierra la tarjeta y abre el libro del mostrador.");
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
        if (action == CasinoAction.ReplayGuide)
        {
            if (!string.IsNullOrEmpty(lastGuide)) Guide(lastGuide);
            return;
        }
        audioManager.SelectAction(action);
        switch (action)
        {
            case CasinoAction.InfoNext: ui.NextInfo(); return;
            case CasinoAction.InfoClose: ui.CloseInfo(); return;
            case CasinoAction.Host:
                if (!inCasino)
                {
                    Show("ANALISTA / ANTECEDENTE BANCARIO\nUna intrusión permitió enviar órdenes de pago fraudulentas.\nControles propuestos: doble autorización y monitoreo de pagos.\nTu tarea aquí es seguir los fondos.\n"+Objective,
                        "Una intrusión permitió enviar órdenes de pago fraudulentas. En el banco, la doble autorización y el monitoreo de pagos son controles propuestos. Tu tarea aquí empieza cuando los fondos llegan a la casa de cambio.");
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
                RegisterEvidence("libro");
                long total; int count;
                bool suspicious = StructuringDetector.Detect(data.operations, data.demonstrationThreshold, out total, out count);
                Show("LIBRO / ORIGEN OR-08\n6 operaciones suman US$58 M.\nUmbral de la demo: US$10 M.\nSiguiente en Defensa: ALERTA de la terminal.", "El libro muestra seis operaciones del mismo origen. El detector suma cincuenta y ocho millones y los compara con el umbral didáctico de diez millones. En Defensa, cierra la tarjeta y revisa la alerta de la terminal. En Ataque, revisa también el comprobante."); return;
            case CasinoAction.Receipt:
                RegisterEvidence("cambio");
                Show("COMPROBANTE / CAMBIO DE MONEDA\nEl registro muestra el cambio de dólares a pesos.\nEn Ataque, completa las tres pistas y pulsa REGISTRAR CAMBIO.", "Este comprobante documenta el cambio de moneda. Completa las tres pistas y pulsa Registrar cambio en la consola."); return;
            case CasinoAction.SharedAlert:
                RegisterEvidence("alerta");
                Show("ALERTA / OR-08 VINCULADO AL CASO\nEste aviso compartido ya existe.\nEn Defensa, tras revisar el LIBRO, pulsa EMITIR REPORTE.", "La alerta compartida vincula el origen O R cero ocho con el incidente. Si estás en Defensa y ya revisaste el libro, cierra la tarjeta y pulsa Emitir reporte en la consola de análisis."); return;
            case CasinoAction.Convert:
                if (aml.frozen) { Show("OPERACION BLOQUEADA\nLos fondos estan congelados. No pueden convertirse."); return; }
                if (!evidence.Contains("libro") || !evidence.Contains("cambio")) { Show("Primero revisa el LIBRO y el COMPROBANTE."); return; }
                converted = true; counter.stage = "Casa de cambio / pesos";
                RegisterEvidence("cambio");
                Show("CAMBIO REGISTRADO / DÓLARES A PESOS\nEl contador cambia de etapa.\nUsa el portal IR AL CASINO, a la derecha.", "El registro ahora marca pesos. Ve al portal Ir al casino para seguir el recorrido."); return;
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
                if (aml.frozen) { Show("PORTAL BLOQUEADO / US$58 M CONGELADOS\nEl ROS impide el ingreso de fondos al casino."); return; }
                if (mode == SimMode.Defensa) { Show("MODO DEFENSA\nEmite el ROS en la casa de cambio antes del casino.\nPara ver el flujo sin controles, elige ATAQUE."); return; }
                if (!converted || evidence.Count < 3) { Show("Antes de cruzar: registra las 3 evidencias\ny pulsa CONVERTIR."); return; }
                inCasino = true; counter.stage = "Casino / fichas"; Teleport(casinoSpawn);
                Show("CASINO / PESOS A FICHAS\nUsa ACERCARSE A LA MESA.\nEn la consola selecciona FICHAS A BANCA y FICHAS A JUGADOR.", "Ya estás en el casino. Usa el círculo para acercarte a la mesa. En la consola coloca un grupo de fichas en Banca y otro en Jugador."); return;
            case CasinoAction.ToExchange:
                if(chipAnimator.running){Show("ESPERA\nTermina de colocar las fichas antes de salir.");return;}
                inCasino=false; Teleport(exchangeSpawn); Show("Regresaste a la casa de cambio.\nEl recorrido conserva las evidencias registradas."); return;
            case CasinoAction.Bet:
                Show("MESA DE BACARA / TU TURNO\nSelecciona COLOCAR EN BANCA y COLOCAR EN JUGADOR.\nDespues revela las cartas. Es una escena educativa, no una partida completa.");return;
            case CasinoAction.CashOut:
                if (!betComplete || aml.frozen) { Show("Coloca ambos grupos de fichas y revela las cartas."); return; }
                counter.cashOut = data.representedCash; counter.stage = "Fichas -> efectivo (tramo representado)";
                Show("CANJE / EFECTIVO REPRESENTADO\nEl contador muestra un valor fijo de la demo.\nPulsa EXPLICAR EL CASO en la consola.",
                    "El contador registra el valor fijo del tramo en efectivo. Pulsa Explicar el caso para identificar el problema de trazabilidad.");return;
            case CasinoAction.ExplainCorrect:
                if (counter.cashOut == 0) { Show("Completa primero la mesa y el canje de fichas."); return; }
                explained = true;
                Show("CORRECTO / LA CLAVE ES EL ORIGEN\nLa demostracion reparte fichas entre lados opuestos.\nNo garantiza ganancias ni vuelve licito el dinero.\nEl control debe relacionar operaciones y origen.\nAhora puedes CERRAR CASO."); return;
            case CasinoAction.ExplainWrong:
                Show("REVISA TU EXPLICACION\nApostar a lados opuestos no garantiza ganar.\nObserva el recorrido: origen -> cambio -> fichas.\nEl problema que estudiamos es perder trazabilidad."); return;
            case CasinoAction.Finish:
                if (mode == SimMode.Ataque && counter.cashOut > 0 && !explained)
                { explaining=true;ui.Refresh(this);return; }
                EndScenario(); return;
            case CasinoAction.Credits:
                Show("CREDITOS / ESCENARIO 08\nEscenario: guia Banco Andino del curso.\nModelos 3D, musica y sonidos originales de esta entrega.\nCardboard: Google, Apache 2.0. Unity/URP: sus licencias.\nImplementacion asistida por ChatGPT/Codex.\nEquipo: completar nombres, roles y fuentes en LEEME."); return;
        }
    }
    public void SidePlaced(bool bank)
    {
        if(bank)bankPlaced=true;else playerPlaced=true;
        ui.Refresh(this);
        if (bankPlaced && playerPlaced)
            Guide("Ya hay fichas en los dos lados. Selecciona Revelar cartas en la consola.");
        else
            Guide(bank ? "Fichas en Banca. Ahora selecciona Fichas a Jugador." : "Fichas en Jugador. Ahora selecciona Fichas a Banca.");
    }
    public void RevealFinished()
    {
        dealt=true;betComplete=true;counter.stage="BANCA 7 / JUGADOR 6";ui.Refresh(this);
        Show("RESULTADO PREPARADO\nBanca 7 / Jugador 6.\nLas fichas ilustran dos resultados opuestos.\nSiguiente: CANJEAR FICHAS.",
            "La escena muestra un resultado preparado, Banca siete y Jugador seis. Selecciona Canjear fichas. El efectivo que verás es una cifra fijada para la demostración.");
    }
    public void EndScenario()
    {
        if (mode == SimMode.Defensa && !aml.frozen) { Show("Falta emitir el ROS para terminar el modo defensa."); return; }
        if (mode == SimMode.Ataque && counter.cashOut == 0) { Show("Completa: evidencias -> convertir -> portal\n-> mesa -> canjear fichas -> cerrar caso."); return; }
        finished=true;
        string outcome = mode == SimMode.Defensa
            ? "DEFENSA / FLUJO DETENIDO\nUS$58 M congelados; US$0 al casino.\nDetector: 6 operaciones, total 58 M.\nValidación: alerta revisada y fondos fuera del casino.\nEl bloqueo es una accion simulada del escenario;\nun reporte por si solo no congela automaticamente."
            : "ATAQUE / PERDIDA DE TRAZABILIDAD\nUS$58 M seguidos; aprox. US$30 M en efectivo\nen el tramo representado (no son ganancias).\nApuestas opuestas reducen exposicion, no el riesgo.\nCambiar fichas no vuelve licito el origen del dinero.";
        string conclusion = outcome + "\nFalla: operaciones aisladas y controles ausentes.\nCompara el otro modo con el boton ATAQUE/DEFENSA.";
        Show(conclusion, mode == SimMode.Defensa
            ? "El programa verificó el patrón del libro, la alerta compartida y que los fondos seguían en la casa de cambio. El reporte y la retención se simulan juntos. Se congelaron cincuenta y ocho millones del ejemplo. El portal quedó bloqueado y cero fondos llegaron al casino."
            : "Terminó el recorrido del dinero. Prueba Defensa para comparar qué ocurre cuando el analista interviene antes del casino.");
        ui.Result(conclusion);
        audioManager.Ending();
    }
}
