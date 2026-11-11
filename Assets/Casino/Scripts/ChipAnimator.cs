using System.Collections;
using UnityEngine;

public class ChipAnimator : MonoBehaviour
{
    public Transform[] chips;
    public Transform bankSpot, playerSpot;
    public bool running;
    Vector3[] homes;
    void Awake()
    {
        homes = new Vector3[chips.Length];
        for (int i = 0; i < chips.Length; i++) homes[i] = chips[i].position;
    }
    public void ResetChips()
    {
        StopAllCoroutines(); running = false;
        if (homes == null) return;
        for (int i = 0; i < chips.Length; i++) chips[i].position = homes[i];
    }
    public void PlaySide(bool bank){if(!running)StartCoroutine(AnimateSide(bank));}
    IEnumerator AnimateSide(bool bank)
    {
        running=true;
        for(int i=bank?0:1;i<chips.Length;i+=2)
        {
            Vector3 from=chips[i].position;
            Vector3 to=(bank?bankSpot.position:playerSpot.position)+Vector3.up*(.055f*(i/2));
            for(float t=0;t<1;t+=Time.deltaTime*3)
            { chips[i].position=Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.35f;yield return null; }
            chips[i].position=to;
        }
        running=false;if(GameManager.I!=null)GameManager.I.SidePlaced(bank);
    }
}
