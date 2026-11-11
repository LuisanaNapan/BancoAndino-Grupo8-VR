using System.Globalization;
using UnityEngine;

public class MoneyCounter : MonoBehaviour
{
    public long frozen;
    public long cashOut;
    public string stage = "Ingreso: dolares";
    public void ResetCounter() { frozen = 0; cashOut = 0; stage = "Ingreso: dolares"; }
    public static string USD(long amount) { return "US$ " + amount.ToString("N0", CultureInfo.InvariantCulture); }
    static string Short(long amount)
    {
        return amount>=1000000?"US$ "+(amount/1000000d).ToString("0.#",CultureInfo.InvariantCulture)+" M":USD(amount);
    }
    public string Summary(ScenarioData data)
    {
        return stage+"\nRuta: "+Short(data.funds)+"\nAparente 'lavado': "+Short(cashOut)+"\nCongelado: "+Short(frozen);
    }
}
