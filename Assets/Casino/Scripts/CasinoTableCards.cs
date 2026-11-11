using UnityEngine;

public class CasinoTableCards : MonoBehaviour
{
    public GameObject[] fronts;
    void Update()
    {
        bool show=GameManager.I!=null&&GameManager.I.CardsRevealed;
        foreach(var f in fronts)if(f!=null&&f.activeSelf!=show)f.SetActive(show);
    }
}
