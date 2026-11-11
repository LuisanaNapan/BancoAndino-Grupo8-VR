using UnityEngine;

public class EvidenceItem : MonoBehaviour
{
    public string evidenceId;
    public void Register() { if (GameManager.I != null) GameManager.I.RegisterEvidence(evidenceId); }
}
