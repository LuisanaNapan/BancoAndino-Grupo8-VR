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
    public float dwellSeconds = 1.8f;
    CasinoInteractable focused;
    float elapsed, yaw, pitch;
    bool consumed;
    bool editorDwell;
    CharacterController walker;
    CasinoHandRig hands;
    float confirmedUntil;
    public CasinoAction? FocusedAction => focused == null ? null : focused.action;
    public float DwellProgress => Mathf.Clamp01(elapsed / Mathf.Max(.1f, dwellSeconds));
    public bool Confirming => Time.unscaledTime < confirmedUntil;
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
        consumed = false;
        if (Application.isEditor && view != null) view.transform.localRotation = Quaternion.identity;
    }
    void Update()
    {
        bool click = false;
        Ray ray = new Ray(view.transform.position, view.transform.forward);
        bool dwell = !Application.isEditor;
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
                if (keyboard.gKey.wasPressedThisFrame) { editorDwell = !editorDwell; elapsed=0; consumed=false; }
                var game = GameManager.I;
                if (game != null)
                {
                    if (keyboard.vKey.wasPressedThisFrame) game.Act(CasinoAction.ReplayGuide);
                    if (keyboard.nKey.wasPressedThisFrame) game.Act(CasinoAction.MuteNarration);
                    if (keyboard.minusKey.wasPressedThisFrame) game.Act(CasinoAction.VoiceDown);
                    if (keyboard.equalsKey.wasPressedThisFrame) game.Act(CasinoAction.VoiceUp);
                    if (keyboard.digit1Key.wasPressedThisFrame) game.ResetScenario(SimMode.Ataque);
                    if (keyboard.digit2Key.wasPressedThisFrame) game.ResetScenario(SimMode.Defensa);
                    if (keyboard.rKey.wasPressedThisFrame) game.ResetScenario(game.mode);
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
        if (Application.isEditor && editorDwell) { dwell=true; ray=new Ray(view.transform.position,view.transform.forward); }
        Select(ray, dwell, click);
    }

    internal void Select(Ray ray, bool dwell, bool click)
    {
        bool hitSurface = Physics.Raycast(ray, out var hit, 13f, ~0, QueryTriggerInteraction.Collide);
        CasinoInteractable candidate = null;
        if(hitSurface)
            candidate=hit.collider.GetComponentInParent<CasinoInteractable>();
        if (candidate != focused)
        {
            if (focused != null) focused.Focus(false);
            focused = candidate; elapsed = 0; consumed = false;
            if (focused != null) focused.Focus(true);
        }
        if (focused != null)
        {
            if (!consumed && dwell) elapsed += Time.deltaTime;
            if (click || (dwell && !consumed && DwellProgress >= 1f))
            {
                consumed = true;
                elapsed = 0;
                confirmedUntil = Time.unscaledTime + .32f;
                focused.Activate();
            }
        }
        float distance = hitSurface ? Mathf.Clamp(hit.distance - .025f, .065f, 2.6f) : 1.8f;
        if (hands != null) hands.Present(ray, distance, candidate != null, DwellProgress, Confirming);
    }
}
