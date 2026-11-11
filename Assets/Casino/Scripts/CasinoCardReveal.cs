using UnityEngine;

public class CasinoCardReveal : MonoBehaviour
{
    public Transform[] cards;
    public GameObject[] fronts,backs;
    float elapsed;bool done;
    void Apply(float t)
    {
        for(int i=0;i<cards.Length;i++)
        {
            cards[i].localRotation=Quaternion.Euler(55,Mathf.Lerp(180,0,Mathf.SmoothStep(0,1,t)),0);
            fronts[i].SetActive(t>=.5f);backs[i].SetActive(t<.5f);
        }
    }
    void Update()
    {
        var g=GameManager.I;if(g==null)return;
        if(!g.RevealRequested){elapsed=0;done=false;Apply(0);return;}
        if(done)return;
        elapsed+=Time.deltaTime;Apply(Mathf.Clamp01(elapsed/1.15f));
        if(elapsed>=1.15f){done=true;g.RevealFinished();}
    }
}
