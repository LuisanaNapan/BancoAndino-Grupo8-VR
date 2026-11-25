using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[DefaultExecutionOrder(-100)]
public class CasinoPointer : MonoBehaviour
{
    public Camera view;
    public Transform reticle;
    public Renderer reticleRenderer;
    public LayerMask interactionMask;
    [Range(.8f, 4f)] public float dwellSeconds = 1.8f;
    [Min(1f)] public float maxDistance = 13f;
    readonly RaycastHit[] hits = new RaycastHit[32];
    CasinoInteractable focused, activated;
    float elapsed, yaw, pitch;
    bool editorDwell;
    CharacterController walker;
    CasinoHandRig hands;
    float confirmedUntil;
    public CasinoAction? FocusedAction => focused == null ? null : focused.action;
    internal CasinoInteractable FocusedTarget => focused;
    public float DwellProgress => Mathf.Clamp01(elapsed / Mathf.Max(.1f, dwellSeconds));
    public bool Confirming => Time.unscaledTime < confirmedUntil;
    public Vector3 AimPoint { get; private set; }
    internal int ActivationCount { get; private set; }
    public bool UsesGaze => !Application.isEditor || editorDwell;
    internal void SetEditorGaze(bool value) { editorDwell = value; elapsed = 0; }
    void Awake()
    {
        // Keep the original XR pose driver on Android; mouse drives the editor only.
        if (Application.isEditor)
        {
            walker=gameObject.GetComponent<CharacterController>();
            if(walker==null)walker=gameObject.AddComponent<CharacterController>();
            walker.height=1.65f;walker.radius=.22f;walker.center=new Vector3(0,-.825f,0);walker.stepOffset=.12f;walker.skinWidth=.02f;
            var pose = view.GetComponent<TrackedPoseDriver>();
            if (pose != null) pose.enabled = false;
        }
    }
    void Start()
    {
        if (reticleRenderer == null) return;
        hands = gameObject.AddComponent<CasinoHandRig>();
        hands.Initialize(view, reticleRenderer);
    }
    public void ResetView()
    {
        yaw = 0;
        pitch = 0;
        if (focused != null) focused.Focus(false);
        focused = null;
        elapsed = 0;
        if (Application.isEditor && view != null) view.transform.localRotation = Quaternion.identity;
    }
    void Update()
    {
        bool click = false;
        Ray ray = new Ray(view.transform.position, view.transform.forward);
        bool dwell = UsesGaze;
        var game = GameManager.I;
        if (game != null && game.Teleporting) return;
        if (Application.isEditor)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * 0.12f; pitch = Mathf.Clamp(pitch - delta.y * 0.12f, -65f, 65f);
                    view.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
                    ray = new Ray(view.transform.position, view.transform.forward);
                }
                else ray = view.ScreenPointToRay(mouse.position.ReadValue());
                click = mouse.leftButton.wasPressedThisFrame;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector3 move=Vector3.zero;
                if(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed)move+=Vector3.forward;
                if(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed)move+=Vector3.back;
                if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)move+=Vector3.left;
                if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)move+=Vector3.right;
                if(move.sqrMagnitude>0 && GameManager.I!=null)
                {
                    move=Quaternion.Euler(0,view.transform.eulerAngles.y,0)*move.normalized;
                    Vector3 step=move*Time.deltaTime*1.8f;
                    if(walker!=null)walker.Move(step);
                    else transform.position+=step;
                }
                if(keyboard.mKey.wasPressedThisFrame && GameManager.I!=null)GameManager.I.audioManager.ToggleMusic();
                if (keyboard.gKey.wasPressedThisFrame) { SetEditorGaze(!editorDwell); dwell = UsesGaze; }
                if (game != null)
                {
                    if (keyboard.vKey.wasPressedThisFrame) game.Act(CasinoAction.ReplayGuide);
                    if (keyboard.nKey.wasPressedThisFrame) game.Act(CasinoAction.MuteNarration);
                    if (keyboard.minusKey.wasPressedThisFrame) game.Act(CasinoAction.VoiceDown);
                    if (keyboard.equalsKey.wasPressedThisFrame) game.Act(CasinoAction.VoiceUp);
                    if (keyboard.digit1Key.wasPressedThisFrame) game.Act(CasinoAction.Attack);
                    if (keyboard.digit2Key.wasPressedThisFrame) game.Act(CasinoAction.Defense);
                    if (keyboard.rKey.wasPressedThisFrame) game.Act(CasinoAction.Restart);
                }
                if (keyboard.spaceKey.wasPressedThisFrame) { click = true; ray = new Ray(view.transform.position, view.transform.forward); }
            }
        }
        else
        {
            // Native Cardboard trigger includes touch input in the installed plugin.
            if (UnityEngine.XR.XRSettings.isDeviceActive) click = Google.XR.Cardboard.Api.IsTriggerPressed;
            else if (Touchscreen.current != null) click = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }
        if(Application.isEditor) ray=new Ray(view.transform.position,ray.direction);
        if (Application.isEditor && editorDwell) ray = new Ray(view.transform.position, view.transform.forward);
        Select(ray, dwell, click);
    }

    internal void Select(Ray ray, bool dwell, bool click)
    {
        var game = GameManager.I;
        if (game != null && game.Teleporting) return;
        float distance = 1.8f;
        float nearest = float.PositiveInfinity;
        CasinoInteractable candidate = null;
        int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.distance >= nearest) continue;
            bool selectableLayer = (interactionMask.value & (1 << hit.collider.gameObject.layer)) != 0;
            var target = selectableLayer ? hit.collider.GetComponentInParent<CasinoInteractable>() : null;
            if (hit.collider.isTrigger && target == null) continue;
            nearest = hit.distance;
            bool ledgerWaiting = target != null && target.action == CasinoAction.Ledger && game != null && !game.ReadyForBook;
            candidate = target != null && target.isActiveAndEnabled && !ledgerWaiting ? target : null;
        }
        if (!float.IsPositiveInfinity(nearest)) distance = Mathf.Max(.055f, nearest - .015f);
        AimPoint = ray.GetPoint(distance);
        if (candidate != activated) activated = null;
        if (candidate != focused)
        {
            if (focused != null) focused.Focus(false);
            focused = candidate;
            elapsed = 0f;
            if (focused != null) focused.Focus(true);
        }
        bool desktopClick = Application.isEditor && !dwell && click;
        if (focused != null && (focused != activated || desktopClick))
        {
            if (dwell) elapsed += Mathf.Min(Time.unscaledDeltaTime, .12f);
            if (click || (dwell && DwellProgress >= 1f))
            {
                activated = focused;
                elapsed = 0f;
                confirmedUntil = Time.unscaledTime + .32f;
                ActivationCount++;
                activated.Activate();
            }
        }
        if (hands != null) hands.Present(ray, distance, candidate != null, DwellProgress, Confirming);
    }
}
