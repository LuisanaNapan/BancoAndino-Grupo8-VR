using UnityEngine;

public class CasinoFreezeVisual : MonoBehaviour
{
    public GameObject barrier;
    void Update()
    {
        bool show=GameManager.I!=null&&GameManager.I.Frozen;
        if(barrier!=null&&barrier.activeSelf!=show)barrier.SetActive(show);
    }
}
