using UnityEngine;

// Gentle breathing light, without rapid flashing or bloom cost.
public class CasinoMarquee : MonoBehaviour
{
    public Renderer[] bulbs;
    MaterialPropertyBlock properties;
    float timer;
    void Awake(){properties=new MaterialPropertyBlock();}
    void Update()
    {
        timer+=Time.deltaTime;
        if(properties==null||bulbs==null)return;
        for(int i=0;i<bulbs.Length;i++)
        {
            if(bulbs[i]==null)continue;
            float strength=.88f+.12f*Mathf.Sin(timer*.9f+i*.12f);
            properties.SetColor("_BaseColor",new Color(strength,strength*.84f,strength*.37f));
            bulbs[i].SetPropertyBlock(properties);
        }
    }
}
