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
    public void ResetView() { yaw = 0; pitch = 0; if (Application.isEditor && view != null) view.transform.localRotation = Quaternion.identity; }
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
                if (keyboard.digit1Key.wasPressedThisFrame) GameManager.I.ResetScenario(SimMode.Ataque);
                if (keyboard.digit2Key.wasPressedThisFrame) GameManager.I.ResetScenario(SimMode.Defensa);
                if (keyboard.rKey.wasPressedThisFrame) GameManager.I.ResetScenario(GameManager.I.mode);
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
        RaycastHit hit;
        CasinoInteractable candidate = null;
        if(GameManager.I!=null && GameManager.I.ui.InfoOpen)
        {
            float best=13f;
            foreach(var uiHit in Physics.RaycastAll(ray,13f,interactionMask,QueryTriggerInteraction.Collide))
            {
                var target=uiHit.collider.GetComponentInParent<CasinoInteractable>();
                if(target!=null&&target.transform.IsChildOf(GameManager.I.ui.infoRoot)&&uiHit.distance<best)
                {candidate=target;best=uiHit.distance;}
            }
        }
        else if(Physics.Raycast(ray,out hit,13f,~0,QueryTriggerInteraction.Collide))
            candidate=hit.collider.GetComponentInParent<CasinoInteractable>();
        if (candidate != focused)
        {
            if (focused != null) focused.Focus(false);
            focused = candidate; elapsed = 0; consumed = false;
            if (focused != null) focused.Focus(true);
        }
        if (reticle != null)
        {
            reticle.position = ray.GetPoint(1.0f);
            float scale = 0.009f + Mathf.Clamp01(elapsed / dwellSeconds) * 0.007f;
            reticle.localScale = Vector3.one * scale;
        }
        if (focused == null) { if (GameManager.I != null) GameManager.I.ui.Prompt("Mira un objeto o el portal"); return; }
        if (!consumed && dwell) elapsed += Time.deltaTime;
        GameManager.I.ui.Prompt(focused.prompt + (dwell && !consumed ? " / " + Mathf.CeilToInt(Mathf.Clamp01(elapsed / dwellSeconds) * 100) + "%" : ""));
        if (click || (dwell && !consumed && elapsed >= dwellSeconds))
        {
            consumed = true; elapsed = 0; focused.Activate();
        }
    }
}
