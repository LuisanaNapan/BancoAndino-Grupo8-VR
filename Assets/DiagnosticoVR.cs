using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

// Temporary, read-only diagnostic overlay. Add to an active scene object.
// Does not initialize XR, change project settings, or enable phone sensors.
[DefaultExecutionOrder(-32000)]
public class DiagnosticoVR : MonoBehaviour
{
    readonly Queue<string> errores = new Queue<string>();
    readonly List<XRDisplaySubsystem> pantallas = new List<XRDisplaySubsystem>();
    readonly List<XRInputSubsystem> entradas = new List<XRInputSubsystem>();
    string informe = "Esperando diagnostico...";
    float proximaActualizacion;

    void Awake()
    {
        Application.logMessageReceived += Capturar;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Capturar;
    }

    void Capturar(string mensaje, string traza, LogType tipo)
    {
        if (tipo != LogType.Error && tipo != LogType.Exception && tipo != LogType.Assert)
            return;
        mensaje = mensaje.Replace("\n", " ").Replace("\r", " ");
        if (mensaje.Length > 220) mensaje = mensaje.Substring(0, 220);
        errores.Enqueue(mensaje);
        while (errores.Count > 3) errores.Dequeue();
    }

    void Update()
    {
        if (Time.unscaledTime < proximaActualizacion) return;
        proximaActualizacion = Time.unscaledTime + 0.5f;
        var texto = new StringBuilder();
        texto.AppendLine("VR-DIAG-URP-02 | " + Application.platform);
        texto.AppendLine("Modelo: " + SystemInfo.deviceModel);
        texto.AppendLine("Render pipeline: " + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "Built-in"));
        texto.AppendLine("Giroscopio detectado: " +
            (UnityEngine.InputSystem.Gyroscope.current != null ? "SI" : "NO"));
        texto.AppendLine("Sensor de orientacion: " +
            (UnityEngine.InputSystem.AttitudeSensor.current != null ? "SI" : "NO"));

        var ajustes = XRGeneralSettings.Instance;
        var manager = ajustes != null ? ajustes.Manager : null;
        if (manager == null)
        {
            texto.AppendLine("XR: no hay XRManagerSettings");
        }
        else
        {
            texto.AppendLine("Inicializacion terminada: " + manager.isInitializationComplete);
            texto.AppendLine("Loader activo: " +
                (manager.activeLoader != null ? manager.activeLoader.name : "NINGUNO"));
            texto.AppendLine("Loaders configurados: " + manager.activeLoaders.Count);
        }

        pantallas.Clear();
        entradas.Clear();
        SubsystemManager.GetSubsystems(pantallas);
        SubsystemManager.GetSubsystems(entradas);
        int pantallasActivas = 0;
        int entradasActivas = 0;
        foreach (var pantalla in pantallas) if (pantalla.running) pantallasActivas++;
        foreach (var entrada in entradas) if (entrada.running) entradasActivas++;
        texto.AppendLine("XR display: " + pantallasActivas + "/" + pantallas.Count + " activos");
        texto.AppendLine("XR input: " + entradasActivas + "/" + entradas.Count + " activos");
        texto.AppendLine("Errores capturados: " + errores.Count);
        foreach (var error in errores) texto.AppendLine("- " + error);
        informe = texto.ToString();
    }

    void OnGUI()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        float escala = Mathf.Max(0.5f, Screen.width / 1000f);
        Matrix4x4 anterior = GUI.matrix;
        Color colorAnterior = GUI.color;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * escala);
        GUI.color = Color.white;
        GUI.Box(new Rect(10, 10, 980, 390), GUIContent.none);
        var estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = 18;
        estilo.wordWrap = true;
        estilo.normal.textColor = Color.white;
        GUI.Label(new Rect(24, 18, 950, 330), informe, estilo);
        if (GUI.Button(new Rect(24, 352, 340, 36), "COPIAR DIAGNOSTICO"))
            GUIUtility.systemCopyBuffer = informe;
        GUI.matrix = anterior;
        GUI.color = colorAnterior;
#endif
    }
}

