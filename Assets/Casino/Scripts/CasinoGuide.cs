using UnityEngine;

public class CasinoGuide : MonoBehaviour
{
    public CasinoInteractable[] targets;
    float elapsed;
    void Update()
    {
        if(GameManager.I==null||targets==null)return;
        elapsed+=Time.deltaTime;
        foreach(var t in targets)
        {
            if(t==null||t.guideMarker==null)continue;
            bool next=GameManager.I.IsNext(t.action);t.guideMarker.SetActive(next);
            if(next){t.guideMarker.transform.localRotation=Quaternion.Euler(0,elapsed*40,0);}
        }
    }
}
