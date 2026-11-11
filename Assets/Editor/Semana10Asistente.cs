using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class Semana10Asistente : EditorWindow
{
    string licencia = "";
    string mensaje = "Instala los requisitos y pulsa Preparar practica.";
    string webcam = "";
    Vector2 scroll;
    public static bool EsVR => File.Exists("Assets/SEMANA10_VR.txt");
    public static string Escena => EsVR ? "Assets/HelloCardboard/Scenes/HelloCardboard.unity" : "Assets/Scenes/PruebaAR.unity";

    [MenuItem("Semana 10/Abrir asistente")]
    public static void Abrir()
    {
        var w = GetWindow<Semana10Asistente>("Semana 10");
        w.minSize = new Vector2(470, 530);
        w.Show();
    }
    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        GUILayout.Label(EsVR ? "PRACTICA VR · HELLO CARDBOARD" : "PRACTICA AR · CUBO SOBRE IMAGEN", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Escena y recursos incluidos. El asistente completa los ajustes que dependen de tu instalacion de Unity.", MessageType.Info);
        GUILayout.Label("1. Preparar", EditorStyles.boldLabel);
        if (EsVR && GUILayout.Button("CONFIGURAR VR CON URP", GUILayout.Height(40)))
            EditorApplication.delayCall += Semana10URPConfig.Configurar;

        if (!EsVR)
        {
            EditorGUILayout.HelpBox("Instala Vuforia 11.4.4 desde el portal oficial. Despues pega aqui tu licencia Basic. Si ya la guardaste, no hace falta pegarla otra vez.", MessageType.None);
            GUILayout.Label("Tu clave de licencia Basic (no se imprime en los informes)");
            licencia = EditorGUILayout.PasswordField(licencia);
            webcam = EditorGUILayout.TextField("Webcam (opcional)", webcam);
            if (GUILayout.Button("Seleccionar primera webcam disponible"))
            {
                var devices = WebCamTexture.devices;
                webcam = devices.Length > 0 ? devices[0].name : "";
                if (devices.Length == 0) mensaje = "No se detecto webcam. Puedes probar directamente en el celular.";
            }
        }
        if (GUILayout.Button("PREPARAR PRACTICA", GUILayout.Height(40))) EjecutarPreparacion();
        GUILayout.Space(14);
        GUILayout.Label("2. Android", EditorStyles.boldLabel);
        if (GUILayout.Button("Cambiar plataforma a Android", GUILayout.Height(32)))
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                mensaje = "Falta Android Build Support. Instalalo desde Unity Hub > Installs > engranaje > Add modules (incluye SDK, NDK y OpenJDK).";
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
        GUILayout.Space(14);
        GUILayout.Label("3. Comprobar y compilar", EditorStyles.boldLabel);
        if (GUILayout.Button("Revisar preparacion"))
        {
            mensaje = Comprobar();
            if (string.IsNullOrEmpty(mensaje)) mensaje = "Ajustes comprobados. Falta compilar y probar en tu celular: esto aun no verifica el funcionamiento real.";
        }
        if (GUILayout.Button("GENERAR APK", GUILayout.Height(36))) EditorApplication.delayCall += GenerarAPK;
        if (GUILayout.Button("Abrir Build Profiles para Build And Run")) EditorApplication.ExecuteMenuItem("File/Build Profiles");
        GUILayout.Space(14);
        EditorGUILayout.HelpBox(mensaje, MessageType.Info);
        EditorGUILayout.HelpBox(EsVR ? "En el celular: vista dividida, giro con giroscopio y toque sobre el tesoro. Usa el QR de tu visor cuando Cardboard lo solicite." : "En el celular: permite la camara y apunta a MarcadorDashboard.jpg abierto en la PC o impreso. El cubo solo se muestra al reconocer la imagen.", MessageType.None);
        GUILayout.Label("NRC 30190 · Practica de la semana 10", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
    }
    void EjecutarPreparacion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { mensaje = "Primero detiene Play."; return; }
        try
        {
            if (!EsVR && !Semana10Inicio.TieneVuforia)
                throw new InvalidOperationException("Primero instala Vuforia. Assets > Import Package > Custom Package, importa el instalador oficial y espera a que termine.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Escena)) throw new FileNotFoundException("Falta la escena de la descarga. Extrae todo el ZIP otra vez.");
            EditorSceneManager.OpenScene(Escena);
            bool cambioInput = ConfigurarAndroid();
            if (EsVR)
            {
                PrepararGradle();
                Invocar("CardboardPreparacion", "Preparar", null);
                int layer = LayerMask.NameToLayer("Interactive");
                if (layer < 0) throw new InvalidOperationException("No se encontro la capa Interactive de la descarga.");
                var treasure = GameObject.Find("Treasure");
                if (treasure == null) throw new InvalidOperationException("La escena no contiene Treasure.");
                foreach (var t in treasure.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                bool encontroReticula = false;
                foreach (var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (m == null || m.GetType().Name != "CardboardReticlePointer") continue;
                    var s = new SerializedObject(m);
                    var mask = s.FindProperty("ReticleInteractionLayerMask");
                    if (mask == null) throw new InvalidOperationException("Cardboard cambio el campo de la reticula. Envia el error.");
                    mask.intValue = 1 << layer;
                    s.ApplyModifiedProperties();
                    encontroReticula = true;
                }
                if (!encontroReticula) throw new InvalidOperationException("No se encontro CardboardReticlePointer en la escena.");
            }
            else
            {
                var importer = AssetImporter.GetAtPath("Assets/Marcadores/MarcadorDashboard.jpg") as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Falta la textura del marcador.");
                if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
                Invocar("VuforiaPreparacion", "Preparar", new object[] { licencia, webcam });
                licencia = "";
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            mensaje = "Preparacion aplicada: escena, ajustes y referencias guardados. Ahora cambia a Android y compila.";
            if (cambioInput) mensaje += "\nSe activo Input System (New): guarda y cierra Unity; vuelve a abrir este mismo proyecto antes de compilar.";
            File.WriteAllText("Preparacion_Semana10.txt", "PRACTICA " + (EsVR ? "VR" : "AR") + "\n" + mensaje + "\nNo es evidencia de prueba en un celular.\n");
        }
        catch (Exception ex)
        {
            var causa = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
            mensaje = causa.Message;
            Debug.LogException(causa);
        }
    }
    static bool ConfigurarAndroid()
    {
        PlayerSettings.companyName = "PracticaSemana10";
        PlayerSettings.productName = EsVR ? "PruebaVR" : "PruebaAR";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, EsVR ? "com.practica.semana10vr" : "com.practica.semana10ar");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = EsVR ? AndroidSdkVersions.AndroidApiLevel26 : AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.Android.forceInternetPermission = EsVR;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        if (!EsVR) return false;
        var s = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = s.FindProperty("activeInputHandler");
        if (input == null) throw new InvalidOperationException("No se encontro Active Input Handling. Selecciona Input System Package (New) en Player Settings y vuelve a preparar.");
        bool cambio = input.intValue != 1;
        input.intValue = 1;
        s.ApplyModifiedProperties();
        return cambio;
    }
    static void PrepararGradle()
    {
        string root = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/Tools/GradleTemplates");
        if (!Directory.Exists(root)) throw new InvalidOperationException("No se encontro Android Build Support en tu editor. Instalalo en Unity Hub junto con SDK, NDK y OpenJDK.");
        Directory.CreateDirectory("Assets/Plugins/Android");
        string main = "Assets/Plugins/Android/mainTemplate.gradle";
        string properties = "Assets/Plugins/Android/gradleTemplate.properties";
        CopiarTemplate(root, "mainTemplate.gradle", main);
        CopiarTemplate(root, "gradleTemplate.properties", properties);
        string text = File.ReadAllText(main);
        if (!text.Contains("**DEPS**")) throw new InvalidOperationException("El mainTemplate.gradle no contiene **DEPS**. No se modifico. Envia este error para adaptar el template.");
        string additions = "";
        foreach (var d in Dependencias)
            if (!text.Contains(d)) additions += "    implementation '" + d + "'\n";
        File.WriteAllText(main, text.Replace("**DEPS**", additions + "**DEPS**"));
        text = File.ReadAllText(properties);
        foreach (var name in new[] { "android.useAndroidX", "android.enableJetifier" })
        {
            string pattern = "(?m)^\\s*" + System.Text.RegularExpressions.Regex.Escape(name) + "\\s*=.*$";
            text = System.Text.RegularExpressions.Regex.IsMatch(text, pattern)
                ? System.Text.RegularExpressions.Regex.Replace(text, pattern, name + "=true")
                : text.TrimEnd() + "\n" + name + "=true\n";
        }
        File.WriteAllText(properties, text);
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        foreach (var name in new[] { "useCustomMainGradleTemplate", "useCustomGradlePropertiesTemplate" })
        {
            var prop = settings.FindProperty(name);
            if (prop == null) throw new InvalidOperationException("No se encontro " + name + ". Marca los dos Custom Gradle Templates en Player > Publishing Settings y vuelve a preparar.");
            if (prop.propertyType == SerializedPropertyType.Boolean) prop.boolValue = true;
            else prop.intValue = 1;
        }
        settings.ApplyModifiedProperties();
        AssetDatabase.Refresh();
    }
    static void CopiarTemplate(string root, string name, string destino)
    {
        if (File.Exists(destino)) return;
        string origen = Path.Combine(root, name);
        if (!File.Exists(origen)) origen = Directory.GetFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
        if (string.IsNullOrEmpty(origen)) throw new FileNotFoundException("Tu editor no contiene " + name + ". Genera ese template en Player > Publishing Settings y repite Preparar practica.");
        File.Copy(origen, destino);
    }
    static readonly string[] Dependencias = {
        "androidx.appcompat:appcompat:1.6.1", "com.google.android.gms:play-services-vision:20.1.3",
        "com.google.android.material:material:1.12.0", "com.google.protobuf:protobuf-javalite:3.19.4"
    };
    static object Invocar(string clase, string metodo, object[] args)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(clase)).FirstOrDefault(t => t != null);
        if (type == null) throw new InvalidOperationException("No esta disponible " + clase + ". Espera a que Unity termine de compilar y abre Semana 10 > Abrir asistente otra vez.");
        var method = type.GetMethod(metodo, BindingFlags.Public | BindingFlags.Static);
        if (method == null) throw new MissingMethodException(clase, metodo);
        return method.Invoke(null, args);
    }
    public static string Comprobar()
    {
        if (SceneManager.GetActiveScene().path == CasinoProyecto.ScenePath) return CasinoProyecto.Validar();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return "Cambia primero la plataforma a Android.";
        if (!File.Exists(Escena)) return "Falta la escena entregada.";
        if (EsVR && !(GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset))
            return "Pulsa CONFIGURAR VR CON URP antes de exportar esta copia.";
        if (SceneManager.GetActiveScene().path != Escena) return "Abre la escena de esta practica o pulsa Preparar practica.";
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (scenes.Length != 1 || scenes[0].path != Escena) return "La lista de escenas no coincide. Pulsa Preparar practica.";
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            return "Falta IL2CPP o ARM64. Pulsa Preparar practica.";
        if (EsVR)
        {
            foreach (var path in new[] { "Assets/Plugins/Android/mainTemplate.gradle", "Assets/Plugins/Android/gradleTemplate.properties" })
                if (!File.Exists(path)) return "Faltan los templates Gradle. Pulsa Preparar practica.";
            foreach (var dep in Dependencias)
                if (!File.ReadAllText("Assets/Plugins/Android/mainTemplate.gradle").Contains(dep)) return "Faltan dependencias Gradle. Pulsa Preparar practica.";
        }
        else if (!Semana10Inicio.TieneVuforia) return "Falta instalar Vuforia.";
        try { return (string)Invocar(EsVR ? "CardboardPreparacion" : "VuforiaPreparacion", "Comprobar", null); }
        catch (Exception ex) { return "No se pudo revisar: " + (ex.InnerException ?? ex).Message; }
    }
    void GenerarAPK()
    {
        string error = Comprobar();
        if (!string.IsNullOrEmpty(error)) { mensaje = error; return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string output = EditorUtility.SaveFilePanel("Guardar APK", "", EsVR ? "PruebaVR" : "PruebaAR", "apk");
        if (string.IsNullOrEmpty(output)) return;
        try
        {
            var report = BuildPipeline.BuildPlayer(new[] { Escena }, output, BuildTarget.Android, BuildOptions.None);
            mensaje = report.summary.result == BuildResult.Succeeded
                ? "APK generado. Instalalo y comprueba el resultado en tu celular."
                : "La compilacion no termino correctamente. Copia el primer error rojo de Console.";
            if (report.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder(output);
        }
        catch (Exception ex) { mensaje = ex.Message; Debug.LogException(ex); }
    }
}

[InitializeOnLoad]
public static class Semana10Inicio
{
    public static bool TieneVuforia => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("Vuforia.VuforiaBehaviour") != null);
    static Semana10Inicio() { EditorApplication.delayCall += Iniciar; }
    static void Iniciar()
    {
        if (File.Exists("Assets/CASINO08.txt")) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Iniciar; return; }
        if (!Semana10Asistente.EsVR && TieneVuforia)
        {
            bool changed = false;
            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone })
            {
                string defs = PlayerSettings.GetScriptingDefineSymbols(target);
                if (!defs.Split(';').Contains("SEMANA10_VUFORIA"))
                {
                    PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defs) ? "SEMANA10_VUFORIA" : defs + ";SEMANA10_VUFORIA");
                    changed = true;
                }
            }
            if (changed) return;
        }
        if (!SessionState.GetBool("Semana10Ventana", false))
        {
            SessionState.SetBool("Semana10Ventana", true);
            if (SceneManager.GetActiveScene().path == "" && !SceneManager.GetActiveScene().isDirty && File.Exists(Semana10Asistente.Escena))
                EditorSceneManager.OpenScene(Semana10Asistente.Escena);
            Semana10Asistente.Abrir();
        }
    }
}

public class Semana10RevisionAntesDeBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 100;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        string error = Semana10Asistente.Comprobar();
        if (!string.IsNullOrEmpty(error)) throw new BuildFailedException("Semana 10: " + error);
    }
}
