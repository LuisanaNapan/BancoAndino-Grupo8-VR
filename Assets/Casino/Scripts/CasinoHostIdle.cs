using UnityEngine;

public class CasinoHostIdle : MonoBehaviour
{
    public Transform head;
    Camera viewer;
    void Start(){viewer=Camera.main;}
    void Update()
    {
        if(head==null||viewer==null)return;
        Vector3 d=viewer.transform.position-head.position;d.y=0;
        if(d.sqrMagnitude>.01f&&d.sqrMagnitude<144f)
            head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(-d),Time.deltaTime*2);
    }
}
