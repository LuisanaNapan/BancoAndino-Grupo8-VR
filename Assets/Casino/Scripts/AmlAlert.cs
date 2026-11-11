using UnityEngine;

public class AmlAlert : MonoBehaviour
{
    public bool frozen;
    public bool TryFreeze(SimMode mode, bool hasPattern, bool hasSharedAlert, bool alreadyInCasino)
    {
        if (mode != SimMode.Defensa || !hasPattern || !hasSharedAlert || alreadyInCasino) return false;
        frozen = true;
        return true;
    }
}
