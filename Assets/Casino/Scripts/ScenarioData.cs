using UnityEngine;

public enum SimMode { Ataque, Defensa }

[CreateAssetMenu(menuName = "Banco Andino/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    public string scenarioTitle = "Casa de cambio y casino";
    public string dates = "5 al 13 de febrero de 2016";
    public long funds = 58000000;
    public long representedCash = 30000000;
    public long demonstrationThreshold = 10000000;
    public long[] operations = { 9500000, 9500000, 9800000, 9800000, 9700000, 9700000 };
    [TextArea] public string disclaimer = "Recreacion educativa. Entidades, origen y operaciones inventados. El umbral y el cambio son parametros didacticos, no reglas legales. Los US$58 M y aproximadamente US$30 M en efectivo proceden de la guia del curso; no representan una ganancia de apuestas.";
}
